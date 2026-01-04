using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Download Controller - Track and manage software download statistics
    /// GET /api/Download/stats - Overall download statistics (Admin)
    /// GET /api/Download/products - Stats by product (Admin)
    /// POST /api/Download/record - Record a new download (Public)
    /// GET /api/Download/history - User's download history (Authenticated)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DownloadController> _logger;
        private readonly ICacheService _cacheService;

        public DownloadController(
            ApplicationDbContext context,
            ILogger<DownloadController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }

        #region Admin Endpoints

        /// <summary>
        /// GET /api/Download/stats
        /// Get overall download statistics
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<DownloadStatsDto>> GetDownloadStats()
        {
            try
            {
                var cacheKey = "download_stats_overall";
                var stats = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    var today = DateTime.UtcNow.Date;
                    var weekStart = today.AddDays(-(int)today.DayOfWeek);
                    var monthStart = new DateTime(today.Year, today.Month, 1);

                    return new DownloadStatsDto
                    {
                        TotalDownloads = await _context.Downloads.CountAsync(),
                        WindowsDownloads = await _context.Downloads
                            .Where(d => d.Platform.ToLower() == "windows").CountAsync(),
                        MacOsDownloads = await _context.Downloads
                            .Where(d => d.Platform.ToLower() == "macos" || d.Platform.ToLower() == "mac").CountAsync(),
                        LinuxDownloads = await _context.Downloads
                            .Where(d => d.Platform.ToLower() == "linux").CountAsync(),
                        TodayDownloads = await _context.Downloads
                            .Where(d => d.DownloadedAt >= today).CountAsync(),
                        ThisWeekDownloads = await _context.Downloads
                            .Where(d => d.DownloadedAt >= weekStart).CountAsync(),
                        ThisMonthDownloads = await _context.Downloads
                            .Where(d => d.DownloadedAt >= monthStart).CountAsync()
                    };
                }, TimeSpan.FromMinutes(5));

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching download stats");
                return StatusCode(500, new { message = "Error fetching stats", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/Download/products
        /// Get download stats grouped by product
        /// </summary>
        [HttpGet("products")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<List<ProductDownloadDto>>> GetProductStats()
        {
            try
            {
                var cacheKey = "download_stats_products";
                var products = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    var downloads = await _context.Downloads
                        .GroupBy(d => d.ProductName)
                        .Select(g => new
                        {
                            ProductName = g.Key,
                            TotalDownloads = g.Count(),
                            LastDownloadDate = g.Max(d => d.DownloadedAt),
                            LatestVersion = g.OrderByDescending(d => d.DownloadedAt).FirstOrDefault()!.Version,
                            Downloads = g.ToList()
                        })
                        .ToListAsync();

                    return downloads.Select(p => new ProductDownloadDto
                    {
                        ProductName = p.ProductName,
                        Version = p.LatestVersion,
                        TotalDownloads = p.TotalDownloads,
                        LastDownloadDate = p.LastDownloadDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        PlatformBreakdown = p.Downloads
                            .GroupBy(d => d.Platform)
                            .ToDictionary(g => g.Key, g => g.Count()),
                        ArchitectureBreakdown = p.Downloads
                            .Where(d => !string.IsNullOrEmpty(d.Architecture))
                            .GroupBy(d => d.Architecture!)
                            .ToDictionary(g => g.Key, g => g.Count())
                    }).ToList();
                }, TimeSpan.FromMinutes(5));

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching product stats");
                return StatusCode(500, new { message = "Error fetching product stats", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/Download/product/{productName}
        /// Get stats for specific product
        /// </summary>
        [HttpGet("product/{productName}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ProductDownloadDto>> GetProductStat(string productName)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(productName);
                
                var downloads = await _context.Downloads
                    .Where(d => d.ProductName.ToLower() == decodedName.ToLower())
                    .ToListAsync();

                if (!downloads.Any())
                {
                    return NotFound(new { message = $"No downloads found for product: {decodedName}" });
                }

                var result = new ProductDownloadDto
                {
                    ProductName = decodedName,
                    Version = downloads.OrderByDescending(d => d.DownloadedAt).FirstOrDefault()?.Version,
                    TotalDownloads = downloads.Count,
                    LastDownloadDate = downloads.Max(d => d.DownloadedAt).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    PlatformBreakdown = downloads
                        .GroupBy(d => d.Platform)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ArchitectureBreakdown = downloads
                        .Where(d => !string.IsNullOrEmpty(d.Architecture))
                        .GroupBy(d => d.Architecture!)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching product stat for {ProductName}", productName);
                return StatusCode(500, new { message = "Error fetching product stat", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/Download/trends
        /// Get download trends over time (last 30 days)
        /// </summary>
        [HttpGet("trends")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<List<DownloadTrendDto>>> GetDownloadTrends([FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-days);
                
                var downloadsData = await _context.Downloads
                    .Where(d => d.DownloadedAt >= startDate)
                    .GroupBy(d => d.DownloadedAt.Date)
                    .Select(g => new 
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToListAsync();

                var downloads = downloadsData.Select(d => new DownloadTrendDto
                {
                    Date = d.Date.ToString("yyyy-MM-dd"),
                    Count = d.Count
                }).ToList();

                return Ok(downloads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching download trends");
                return StatusCode(500, new { message = "Error fetching trends", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/Download/all
        /// Get all downloads with pagination (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<object>> GetAllDownloads(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? product = null,
            [FromQuery] string? platform = null)
        {
            try
            {
                var query = _context.Downloads.AsQueryable();

                if (!string.IsNullOrEmpty(product))
                    query = query.Where(d => d.ProductName.ToLower().Contains(product.ToLower()));

                if (!string.IsNullOrEmpty(platform))
                    query = query.Where(d => d.Platform.ToLower() == platform.ToLower());

                var total = await query.CountAsync();
                
                var downloads = await query
                    .OrderByDescending(d => d.DownloadedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DownloadHistoryDto
                    {
                        Id = d.Id,
                        ProductName = d.ProductName,
                        Version = d.Version,
                        Platform = d.Platform,
                        Architecture = d.Architecture,
                        DownloadedAt = d.DownloadedAt,
                        FileSize = d.FileSize,
                        DownloadCompleted = d.DownloadCompleted
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    downloads
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching all downloads");
                return StatusCode(500, new { message = "Error fetching downloads", error = ex.Message });
            }
        }

        #endregion

        #region Public Endpoints

        /// <summary>
        /// POST /api/Download/record
        /// Record a new download (publicly accessible)
        /// </summary>
        [HttpPost("record")]
        [AllowAnonymous]
        public async Task<ActionResult<RecordDownloadResponseDto>> RecordDownload([FromBody] RecordDownloadDto request)
        {
            try
            {
                // Get IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ipAddress = Request.Headers["X-Forwarded-For"].ToString().Split(',').FirstOrDefault()?.Trim();
                }

                // Get User-Agent
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Get user info if authenticated
                string? userId = null;
                string? userEmail = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                // Auto-detect platform from User-Agent if not provided
                var platform = request.Platform;
                if (string.IsNullOrEmpty(platform) && !string.IsNullOrEmpty(userAgent))
                {
                    platform = DetectPlatformFromUserAgent(userAgent);
                }

                // Get referrer
                var referrer = Request.Headers["Referer"].ToString();

                var download = new Download
                {
                    ProductName = request.ProductName,
                    Version = request.Version,
                    Platform = platform,
                    Architecture = request.Architecture,
                    UserId = userId,
                    UserEmail = userEmail,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                    DownloadedAt = DateTime.UtcNow,
                    FileSize = request.FileSize,
                    DownloadCompleted = true,
                    Referrer = referrer?.Length > 100 ? referrer.Substring(0, 100) : referrer,
                    DownloadSource = (!string.IsNullOrEmpty(request.DownloadSource) && request.DownloadSource.Length > 500) 
                        ? request.DownloadSource.Substring(0, 500) 
                        : (request.DownloadSource ?? "website")
                };

                _context.Downloads.Add(download);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cacheService.Remove("download_stats_overall");
                _cacheService.Remove("download_stats_products");

                _logger.LogInformation("📥 Download recorded: {Product} ({Platform}) from {IP}",
                    request.ProductName, platform, ipAddress);

                return Ok(new RecordDownloadResponseDto
                {
                    Success = true,
                    Message = "Download recorded successfully",
                    DownloadId = download.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error recording download");
                return StatusCode(500, new RecordDownloadResponseDto
                {
                    Success = false,
                    Message = "Error recording download"
                });
            }
        }

        /// <summary>
        /// POST /api/Download/complete/{downloadId}
        /// Mark download as completed
        /// </summary>
        [HttpPost("complete/{downloadId}")]
        [AllowAnonymous]
        public async Task<IActionResult> MarkDownloadComplete(int downloadId)
        {
            try
            {
                var download = await _context.Downloads.FindAsync(downloadId);
                if (download == null)
                {
                    return NotFound(new { message = "Download not found" });
                }

                download.DownloadCompleted = true;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Download marked as complete" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marking download as complete");
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }

        #endregion

        #region Authenticated User Endpoints

        /// <summary>
        /// GET /api/Download/history
        /// Get download history for current user
        /// </summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<List<DownloadHistoryDto>>> GetDownloadHistory()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var downloads = await _context.Downloads
                    .Where(d => d.UserEmail == userEmail)
                    .OrderByDescending(d => d.DownloadedAt)
                    .Select(d => new DownloadHistoryDto
                    {
                        Id = d.Id,
                        ProductName = d.ProductName,
                        Version = d.Version,
                        Platform = d.Platform,
                        Architecture = d.Architecture,
                        DownloadedAt = d.DownloadedAt,
                        FileSize = d.FileSize,
                        DownloadCompleted = d.DownloadCompleted
                    })
                    .ToListAsync();

                return Ok(downloads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching download history");
                return StatusCode(500, new { message = "Error fetching history", error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Detect platform from User-Agent string
        /// </summary>
        private string DetectPlatformFromUserAgent(string userAgent)
        {
            var ua = userAgent.ToLower();
            
            if (ua.Contains("windows"))
                return "Windows";
            if (ua.Contains("mac os") || ua.Contains("macos") || ua.Contains("macintosh"))
                return "macOS";
            if (ua.Contains("linux"))
                return "Linux";
            if (ua.Contains("android"))
                return "Android";
            if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ios"))
                return "iOS";
            
            return "Unknown";
        }

        #endregion
    }
}
