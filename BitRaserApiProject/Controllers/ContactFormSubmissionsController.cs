using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using System.Text.Json.Serialization;

namespace BitRaserApiProject.Controllers
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

        public ContactFormSubmissionsController(
            ApplicationDbContext context,
            ILogger<ContactFormSubmissionsController> logger)
        {
            _context = context;
            _logger = logger;
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
