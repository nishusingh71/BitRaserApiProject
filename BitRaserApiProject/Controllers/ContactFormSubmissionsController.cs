using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureApi.Models;
using System.Text.Json.Serialization;
using DSecureApi.Services;
using DSecureApi.Services.Email;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Controller for managing contact form submissions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContactFormSubmissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactFormSubmissionsController> _logger;
        private readonly ICacheService _cacheService;
        private readonly IEmailOrchestrator? _emailOrchestrator;
        private const string TEAM_EMAIL = "Support@dsecuretech.com";

        public ContactFormSubmissionsController(
            ApplicationDbContext context,
            ILogger<ContactFormSubmissionsController> logger,
            ICacheService cacheService,
            IEmailOrchestrator? emailOrchestrator = null)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
            _emailOrchestrator = emailOrchestrator;
        }

        /// <summary>
        /// Submit a new contact form
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ContactFormSubmissionResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormSubmissionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // Create entity from DTO
                var submission = new ContactFormSubmission
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Company = dto.Company,
                    Phone = dto.Phone,
                    Country = dto.Country,
                    BusinessType = dto.BusinessType,
                    SolutionType = dto.SolutionType,
                    ComplianceRequirements = dto.ComplianceRequirements,
                    Message = dto.Message,
                    UsageType = dto.UsageType,
                    Source = dto.Source ?? "Contact Page",
                    SubmittedAt = !string.IsNullOrEmpty(dto.Timestamp) 
                        ? DateTime.Parse(dto.Timestamp).ToUniversalTime() 
                        : DateTime.UtcNow,
                    Status = "pending",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.ContactFormSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New contact form submission from {Email} - ID: {Id}", 
                    submission.Email, submission.Id);

                // ═══════════════════════════════════════════════════════════════
                // EMAIL AUTOMATION - Send notifications via hybrid email system
                // ═══════════════════════════════════════════════════════════════
                if (_emailOrchestrator != null)
                {
                    try
                    {
                        // 1. Send internal team notification
                        await SendTeamNotificationAsync(submission);
                        
                        // 2. Send auto-response to user
                        await SendUserAutoResponseAsync(submission);
                    }
                    catch (Exception emailEx)
                    {
                        // Don't fail the submission if email fails
                        _logger.LogWarning(emailEx, "Failed to send contact form emails for submission {Id}", submission.Id);
                    }
                }

                var response = MapToResponse(submission);

                return CreatedAtAction(
                    nameof(GetSubmissionById), 
                    new { id = submission.Id }, 
                    new ApiResponse<ContactFormSubmissionResponse>
                    {
                        Success = true,
                        Message = "Contact form submitted successfully",
                        Data = response
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact form");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while submitting the form",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all contact form submissions (with pagination)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ContactFormSubmissionResponse>>), 200)]
        public async Task<IActionResult> GetAllSubmissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] bool? isRead = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.ContactFormSubmissions.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                if (isRead.HasValue)
                {
                    query = query.Where(c => c.IsRead == isRead.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => 
                        c.Name.Contains(search) || 
                        c.Email.Contains(search) || 
                        c.Company != null && c.Company.Contains(search) ||
                        c.Message.Contains(search));
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(c => c.SubmittedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(c => c.SubmittedAt <= toDate.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination and ordering
                var submissions = await query
                    .OrderByDescending(c => c.SubmittedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new PaginatedResult<ContactFormSubmissionResponse>
                {
                    Items = submissions.Select(MapToResponse).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<ContactFormSubmissionResponse>>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact form submissions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fetching submissions"
                });
            }
        }

        /// <summary>
        /// Get a single contact form submission by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ContactFormSubmissionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetSubmissionById(int id)
        {
            try
            {
                var submission = await _context.ContactFormSubmissions.FindAsync(id);
                
                if (submission == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Contact form submission with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<ContactFormSubmissionResponse>
                {
                    Success = true,
                    Data = MapToResponse(submission)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact form submission {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fetching the submission"
                });
            }
        }

        /// <summary>
        /// Update contact form submission status
        /// </summary>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ContactFormSubmissionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateSubmission(int id, [FromBody] UpdateContactFormStatusDto dto)
        {
            try
            {
                var submission = await _context.ContactFormSubmissions.FindAsync(id);
                
                if (submission == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Contact form submission with ID {id} not found"
                    });
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(dto.Status))
                {
                    submission.Status = dto.Status;
                }

                if (dto.Notes != null)
                {
                    submission.Notes = dto.Notes;
                }

                if (dto.IsRead.HasValue)
                {
                    submission.IsRead = dto.IsRead.Value;
                    if (dto.IsRead.Value && !submission.ReadAt.HasValue)
                    {
                        submission.ReadAt = DateTime.UtcNow;
                        // Get admin email from context if authenticated
                        submission.ReadBy = User.Identity?.Name;
                    }
                }

                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated contact form submission {Id}", id);

                return Ok(new ApiResponse<ContactFormSubmissionResponse>
                {
                    Success = true,
                    Message = "Submission updated successfully",
                    Data = MapToResponse(submission)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact form submission {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the submission"
                });
            }
        }

        /// <summary>
        /// Mark submission as read
        /// </summary>
        [HttpPost("{id}/read")]
        [ProducesResponseType(typeof(ApiResponse<ContactFormSubmissionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var submission = await _context.ContactFormSubmissions.FindAsync(id);
                
                if (submission == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Contact form submission with ID {id} not found"
                    });
                }

                submission.IsRead = true;
                submission.ReadAt = DateTime.UtcNow;
                submission.ReadBy = User.Identity?.Name;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<ContactFormSubmissionResponse>
                {
                    Success = true,
                    Message = "Submission marked as read",
                    Data = MapToResponse(submission)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking submission {Id} as read", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Delete a contact form submission
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            try
            {
                var submission = await _context.ContactFormSubmissions.FindAsync(id);
                
                if (submission == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Contact form submission with ID {id} not found"
                    });
                }

                _context.ContactFormSubmissions.Remove(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted contact form submission {Id}", id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Submission deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact form submission {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the submission"
                });
            }
        }

        /// <summary>
        /// Get submission statistics
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<ContactFormStats>), 200)]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var lastWeek = today.AddDays(-7);
                var lastMonth = today.AddMonths(-1);

                var stats = new ContactFormStats
                {
                    TotalSubmissions = await _context.ContactFormSubmissions.CountAsync(),
                    PendingSubmissions = await _context.ContactFormSubmissions
                        .CountAsync(c => c.Status == "pending"),
                    UnreadSubmissions = await _context.ContactFormSubmissions
                        .CountAsync(c => !c.IsRead),
                    TodaySubmissions = await _context.ContactFormSubmissions
                        .CountAsync(c => c.SubmittedAt >= today),
                    WeekSubmissions = await _context.ContactFormSubmissions
                        .CountAsync(c => c.SubmittedAt >= lastWeek),
                    MonthSubmissions = await _context.ContactFormSubmissions
                        .CountAsync(c => c.SubmittedAt >= lastMonth),
                    ByStatus = await _context.ContactFormSubmissions
                        .GroupBy(c => c.Status)
                        .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    ByBusinessType = await _context.ContactFormSubmissions
                        .Where(c => c.BusinessType != null)
                        .GroupBy(c => c.BusinessType!)
                        .Select(g => new BusinessTypeCount { BusinessType = g.Key, Count = g.Count() })
                        .ToListAsync()
                };

                return Ok(new ApiResponse<ContactFormStats>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact form stats");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fetching statistics"
                });
            }
        }

        #region Helper Methods

        private ContactFormSubmissionResponse MapToResponse(ContactFormSubmission entity)
        {
            return new ContactFormSubmissionResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Company = entity.Company,
                Phone = entity.Phone,
                Country = entity.Country,
                BusinessType = entity.BusinessType,
                SolutionType = entity.SolutionType,
                ComplianceRequirements = entity.ComplianceRequirements,
                Message = entity.Message,
                UsageType = entity.UsageType,
                Source = entity.Source,
                SubmittedAt = entity.SubmittedAt,
                Status = entity.Status,
                IsRead = entity.IsRead,
                ReadAt = entity.ReadAt,
                ReadBy = entity.ReadBy,
                Notes = entity.Notes
            };
        }

        private string? GetClientIpAddress()
        {
            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Send notification email to internal team (Support@dsecuretech.com)
        /// </summary>
        private async Task SendTeamNotificationAsync(ContactFormSubmission submission)
        {
            if (_emailOrchestrator == null) return;

            var emailRequest = new EmailSendRequest
            {
                ToEmail = TEAM_EMAIL,
                ToName = "DSecure Support Team",
                Subject = $"📩 New Contact Form: {submission.Name} - {submission.BusinessType ?? "General"}",
                HtmlBody = GenerateTeamNotificationHtml(submission),
                Type = EmailType.Notification
            };

            var result = await _emailOrchestrator.SendEmailAsync(emailRequest);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Team notification sent for submission {Id} via {Provider}", 
                    submission.Id, result.ProviderUsed);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to send team notification for submission {Id}: {Message}", 
                    submission.Id, result.Message);
            }
        }

        /// <summary>
        /// Send auto-response email to the user who submitted the form
        /// </summary>
        private async Task SendUserAutoResponseAsync(ContactFormSubmission submission)
        {
            if (_emailOrchestrator == null) return;

            var emailRequest = new EmailSendRequest
            {
                ToEmail = submission.Email,
                ToName = submission.Name,
                Subject = "Thank you for contacting DSecure Technologies",
                HtmlBody = GenerateUserAutoResponseHtml(submission.Name),
                Type = EmailType.Notification
            };

            var result = await _emailOrchestrator.SendEmailAsync(emailRequest);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Auto-response sent to {Email} via {Provider}", 
                    submission.Email, result.ProviderUsed);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to send auto-response to {Email}: {Message}", 
                    submission.Email, result.Message);
            }
        }

        /// <summary>
        /// Generate HTML for team notification email
        /// </summary>
        private string GenerateTeamNotificationHtml(ContactFormSubmission s)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 25px;'>
<h1 style='margin: 0; font-size: 20px;'>📩 New Contact Form Submission</h1>
<p style='margin: 5px 0 0 0; opacity: 0.8;'>Submitted: {s.SubmittedAt:yyyy-MM-dd HH:mm} UTC</p>
</div>
<div style='padding: 25px;'>
<table style='width: 100%; border-collapse: collapse;'>
<tr><td style='padding: 8px 0; color: #666;'><strong>Name:</strong></td><td>{s.Name}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Email:</strong></td><td><a href='mailto:{s.Email}'>{s.Email}</a></td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Company:</strong></td><td>{s.Company ?? "Not provided"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Phone:</strong></td><td>{s.Phone ?? "Not provided"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Country:</strong></td><td>{s.Country ?? "Not provided"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Business Type:</strong></td><td>{s.BusinessType ?? "Not specified"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Solution Type:</strong></td><td>{s.SolutionType ?? "Not specified"}</td></tr>
</table>
<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin-top: 20px;'>
<strong>Message:</strong>
<p style='margin: 10px 0 0 0; white-space: pre-wrap;'>{s.Message}</p>
</div>
</div>
<div style='background: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
Submission ID: #{s.Id} • Source: {s.Source}
</div>
</div>
</body></html>";
        }

        /// <summary>
        /// Generate HTML for user auto-response email
        /// </summary>
        private string GenerateUserAutoResponseHtml(string userName)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 30px; text-align: center;'>
<h1 style='margin: 0; font-size: 24px;'>Thank You for Contacting Us!</h1>
</div>
<div style='padding: 30px;'>
<p style='font-size: 16px;'>Dear {userName},</p>
<p>Thank you for reaching out to DSecure Technologies. We have received your message and appreciate you taking the time to contact us.</p>
<div style='background: #e8f5e9; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #4caf50;'>
<p style='margin: 0; font-weight: 500;'>⏰ Our team will get back to you within 24 hours.</p>
</div>
<p>In the meantime, if you have any urgent queries, feel free to reach us at:</p>
<p>📧 <a href='mailto:Support@dsecuretech.com'>Support@dsecuretech.com</a></p>
<p style='margin-top: 25px;'>Best regards,<br><strong>DSecure Technologies Team</strong></p>
</div>
<div style='background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #888;'>
© 2024 DSecure Technologies. All rights reserved.<br>
<a href='https://dsecuretech.com' style='color: #1a1a2e;'>www.dsecuretech.com</a>
</div>
</div>
</body></html>";
        }

        #endregion
    }

    #region Response Models

    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }

    public class PaginatedResult<T>
    {
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
    }

    public class ContactFormStats
    {
        [JsonPropertyName("totalSubmissions")]
        public int TotalSubmissions { get; set; }

        [JsonPropertyName("pendingSubmissions")]
        public int PendingSubmissions { get; set; }

        [JsonPropertyName("unreadSubmissions")]
        public int UnreadSubmissions { get; set; }

        [JsonPropertyName("todaySubmissions")]
        public int TodaySubmissions { get; set; }

        [JsonPropertyName("weekSubmissions")]
        public int WeekSubmissions { get; set; }

        [JsonPropertyName("monthSubmissions")]
        public int MonthSubmissions { get; set; }

        [JsonPropertyName("byStatus")]
        public List<StatusCount> ByStatus { get; set; } = new();

        [JsonPropertyName("byBusinessType")]
        public List<BusinessTypeCount> ByBusinessType { get; set; } = new();
    }

    public class StatusCount
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class BusinessTypeCount
    {
        [JsonPropertyName("businessType")]
        public string BusinessType { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    #endregion
}
