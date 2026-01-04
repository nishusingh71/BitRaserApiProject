using System.Security.Claims;
using System.Net;

namespace BitRaserApiProject.Middleware
{
    public class IpBindingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpBindingMiddleware> _logger;

        public IpBindingMiddleware(RequestDelegate next, ILogger<IpBindingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tokenIp = context.User.FindFirst("ip_address")?.Value;
                
                if (!string.IsNullOrEmpty(tokenIp))
                {
                    var requestIp = context.Connection.RemoteIpAddress?.ToString();

                    // Handle Proxy/Ngrok cases (X-Forwarded-For) if needed, 
                    // but RemoteIpAddress is usually populated correctly by ASP.NET Core if forwarders are configured.
                    // For strict binding, we compare what we see now vs what we saw at login.
                    
                    if (requestIp != tokenIp)
                    {
                        // Allow localhost loopback variants or same subnet if needed, 
                        // but for strict security, exact match is best.
                        
                        // Special case: Handle ::1 vs 127.0.0.1 mismatch on local dev
                        bool isLocalMismatch = (requestIp == "::1" && tokenIp == "127.0.0.1") || 
                                             (requestIp == "127.0.0.1" && tokenIp == "::1");

                        if (!isLocalMismatch)
                        {
                            _logger.LogWarning("🚨 IP Binding Mismatch! Token issued to {TokenIp} but used from {RequestIp}. Blocking request.", 
                                tokenIp, requestIp);
                            
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            await context.Response.WriteAsJsonAsync(new { message = "Session invalid: IP address mismatch. Please login again." });
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
