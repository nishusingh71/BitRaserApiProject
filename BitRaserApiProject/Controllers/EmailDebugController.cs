using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Utilities;
using System.Security.Claims;
using BitRaserApiProject.Factories;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Debug controller for testing email encoding/decoding
    /// Use this to verify Base64 email functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmailDebugController : ControllerBase
    {
        private readonly ILogger<EmailDebugController> _logger;
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ICacheService _cacheService;

        public EmailDebugController(
            ILogger<EmailDebugController> logger,
            DynamicDbContextFactory contextFactory,
            ICacheService cacheService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Test endpoint: Encode email to Base64
        /// GET /api/EmailDebug/encode/{email}
        /// </summary>
        [HttpGet("encode/{email}")]
        [AllowAnonymous]
        public IActionResult EncodeEmail(string email)
        {
            try
            {
                if (!Base64EmailEncoder.IsValidEmail(email))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid email format",
                        providedEmail = email
                    });
                }

                var encoded = Base64EmailEncoder.Encode(email);

                return Ok(new
                {
                    success = true,
                    plainEmail = email,
                    base64Email = encoded,
                    usage = new
                    {
                        swagger = $"Use this in Swagger: {encoded}",
                        javascript = $"const encoded = btoa('{email}').replace(/\\+/g, '-').replace(/\\//g, '_').replace(/=+$/, '');",
                        csharp = $"var encoded = Base64EmailEncoder.Encode(\"{email}\");"
                    },
                    testEndpoints = new
                    {
                        getUser = $"/api/Users/{encoded}",
                        getSubusers = $"/api/EnhancedSubuser/by-parent/{encoded}",
                        getSessions = $"/api/Sessions/by-email/{encoded}"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error encoding email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test endpoint: Decode Base64 email
        /// GET /api/EmailDebug/decode/{encodedEmail}
        /// </summary>
        [HttpGet("decode/{encodedEmail}")]
        [AllowAnonymous]
        public IActionResult DecodeEmail(string encodedEmail)
        {
            try
            {
                var decoded = Base64EmailEncoder.Decode(encodedEmail);

                if (!Base64EmailEncoder.IsValidEmail(decoded))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Decoded value is not a valid email",
                        decodedValue = decoded
                    });
                }

                return Ok(new
                {
                    success = true,
                    base64Email = encodedEmail,
                    plainEmail = decoded,
                    isValid = Base64EmailEncoder.IsValidEmail(decoded)
                });
            }
            catch (FormatException)
            {
                // Check if it's already a plain email
                if (Base64EmailEncoder.IsValidEmail(encodedEmail))
                {
                    return Ok(new
                    {
                        success = true,
                        message = "This is already a plain email (not Base64)",
                        plainEmail = encodedEmail,
                        shouldBe = Base64EmailEncoder.Encode(encodedEmail),
                        note = "For API calls, encode this first"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Base64 encoding",
                    providedValue = encodedEmail,
                    hint = "Use /api/EmailDebug/encode/{email} to get correct encoding"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error decoding email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test endpoint: Check if email parameter will work
        /// GET /api/EmailDebug/test/{emailOrEncoded}
        /// </summary>
        [HttpGet("test/{emailOrEncoded}")]
        [AllowAnonymous]
        public IActionResult TestEmailParameter(string emailOrEncoded)
        {
            var result = new
            {
                inputValue = emailOrEncoded,
                isPlainEmail = Base64EmailEncoder.IsValidEmail(emailOrEncoded),
                tests = new List<object>()
            };

            // Test 1: Is it a plain email?
            if (Base64EmailEncoder.IsValidEmail(emailOrEncoded))
            {
                result.tests.Add(new
                {
                    test = "Plain Email Detection",
                    status = "✅ PASS",
                    message = "This is a valid plain email",
                    recommendation = "Encode before using in API calls",
                    encoded = Base64EmailEncoder.Encode(emailOrEncoded)
                });
            }
            else
            {
                result.tests.Add(new
                {
                    test = "Plain Email Detection",
                    status = "❌ FAIL",
                    message = "This is not a plain email"
                });

                // Test 2: Can it be decoded as Base64?
                try
                {
                    var decoded = Base64EmailEncoder.Decode(emailOrEncoded);
                    
                    if (Base64EmailEncoder.IsValidEmail(decoded))
                    {
                        result.tests.Add(new
                        {
                            test = "Base64 Decoding",
                            status = "✅ PASS",
                            message = "Successfully decoded to valid email",
                            decodedEmail = decoded,
                            recommendation = "This value is ready for API use"
                        });
                    }
                    else
                    {
                        result.tests.Add(new
                        {
                            test = "Base64 Decoding",
                            status = "⚠️ WARNING",
                            message = "Decoded but not a valid email",
                            decodedValue = decoded
                        });
                    }
                }
                catch (FormatException)
                {
                    result.tests.Add(new
                    {
                        test = "Base64 Decoding",
                        status = "❌ FAIL",
                        message = "Not valid Base64 encoding",
                        recommendation = "Provide either plain email or Base64-encoded email"
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Get current authenticated user's email
        /// Useful for debugging JWT claims
        /// </summary>
        [HttpGet("current-user")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                success = true,
                currentUser = new
                {
                    email = email,
                    emailClaim = emailClaim,
                    role = role,
                    allClaims = User.Claims.Select(c => new
                    {
                        type = c.Type,
                        value = c.Value
                    }).ToList()
                },
                encodedEmail = email != null ? Base64EmailEncoder.Encode(email) : null,
                testEndpoints = email != null ? new
                {
                    getSubusers = $"/api/EnhancedSubuser/by-parent/{Base64EmailEncoder.Encode(email)}",
                    getUserSessions = $"/api/Sessions/by-email/{Base64EmailEncoder.Encode(email)}",
                    getMachines = $"/api/Machines/by-email/{Base64EmailEncoder.Encode(email)}"
                } : null
            });
        }

        /// <summary>
        /// Comprehensive test of all email endpoints
        /// Tests if email parameter works in different scenarios
        /// </summary>
        [HttpPost("test-all")]
        [AllowAnonymous]
        public IActionResult TestAllScenarios([FromBody] EmailTestRequest request)
        {
            var results = new List<object>();

            // Test 1: Plain email encoding
            try
            {
                var encoded = Base64EmailEncoder.Encode(request.Email);
                results.Add(new
                {
                    test = "Encoding Plain Email",
                    status = "✅ PASS",
                    input = request.Email,
                    output = encoded
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    test = "Encoding Plain Email",
                    status = "❌ FAIL",
                    error = ex.Message
                });
            }

            // Test 2: Decode and re-encode (round trip)
            try
            {
                var encoded = Base64EmailEncoder.Encode(request.Email);
                var decoded = Base64EmailEncoder.Decode(encoded);
                var matches = decoded == request.Email;

                results.Add(new
                {
                    test = "Round Trip (Encode → Decode)",
                    status = matches ? "✅ PASS" : "❌ FAIL",
                    original = request.Email,
                    encoded = encoded,
                    decoded = decoded,
                    matches = matches
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    test = "Round Trip Test",
                    status = "❌ FAIL",
                    error = ex.Message
                });
            }

            // Test 3: Email validation
            try
            {
                var isValid = Base64EmailEncoder.IsValidEmail(request.Email);
                results.Add(new
                {
                    test = "Email Validation",
                    status = isValid ? "✅ PASS" : "❌ FAIL",
                    email = request.Email,
                    isValid = isValid
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    test = "Email Validation",
                    status = "❌ FAIL",
                    error = ex.Message
                });
            }

            return Ok(new
            {
                success = true,
                testedEmail = request.Email,
                totalTests = results.Count,
                passed = results.Count(r => r.ToString()!.Contains("✅")),
                results = results
            });
        }

        public class EmailTestRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        /// <summary>
        /// Check database for subusers - DEBUGGING ONLY
        /// GET /api/EmailDebug/check-database/{parentEmail}
        /// </summary>
        [HttpGet("check-database/{parentEmail}")]
        [Authorize]
        public async Task<IActionResult> CheckDatabase(string parentEmail)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("🔍 Checking database for parent email: {ParentEmail}", parentEmail);

                // Get all subusers for this parent
                var subusers = await context.subuser
                    .Where(s => s.user_email == parentEmail)
                    .Select(s => new
                    {
                        s.subuser_id,
                        s.subuser_email,
                        s.user_email,
                        s.Name
                    })
                    .ToListAsync();

                // Get all unique parent emails
                var allParentEmails = await context.subuser
                    .Select(s => s.user_email)
                    .Distinct()
                    .ToListAsync();

                // Get total subusers count
                var totalSubusers = await context.subuser.CountAsync();

                // Check if exact match exists
                var exactMatch = allParentEmails.Any(e => e == parentEmail);

                // Check case-insensitive match
                var caseInsensitiveMatch = allParentEmails
                    .FirstOrDefault(e => e.Equals(parentEmail, StringComparison.OrdinalIgnoreCase));

                return Ok(new
                {
                    success = true,
                    searchedParentEmail = parentEmail,
                    foundSubusers = subusers.Count,
                    subusers = subusers,
                    databaseInfo = new
                    {
                        totalSubusersInDb = totalSubusers,
                        totalUniqueParents = allParentEmails.Count,
                        allParentEmails = allParentEmails,
                        exactMatch = exactMatch,
                        caseInsensitiveMatch = caseInsensitiveMatch != null,
                        matchedEmail = caseInsensitiveMatch
                    },
                    debugging = new
                    {
                        searchQuery = $"WHERE user_email = '{parentEmail}'",
                        hint = exactMatch ? 
                            "✅ Exact match found" : 
                            caseInsensitiveMatch != null ? 
                                $"⚠️ Case mismatch: Database has '{caseInsensitiveMatch}', you searched '{parentEmail}'" :
                                $"❌ No match found. Available parent emails: {string.Join(", ", allParentEmails)}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking database",
                    error = ex.Message
                });
            }
        }
    }
}
