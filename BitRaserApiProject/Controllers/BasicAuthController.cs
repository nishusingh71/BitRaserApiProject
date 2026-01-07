using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using DSecureApi.Services;

namespace DSecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasicAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public BasicAuthController(ApplicationDbContext context, IConfiguration configuration, ICacheService cacheService)
        {
            _context = context;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        public class BasicLoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] BasicLoginRequest login)
        {
            if (!await IsValidUserAsync(login.Email, login.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            // Generate JWT token here (your existing token generation code)
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var token = GenerateJwtToken(login.Email, ipAddress);

            return Ok(new { token });
        }

        private async Task<bool> IsValidUserAsync(string email, string password)
        {
            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();
            if (user == null)
                return false;

            // Verify hashed password using BCrypt
            return !string.IsNullOrEmpty(user.hash_password) && BCrypt.Net.BCrypt.Verify(password, user.hash_password);
        }

        private string GenerateJwtToken(string username, string ipAddress)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT secret key is not configured");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("ip_address", ipAddress)
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}