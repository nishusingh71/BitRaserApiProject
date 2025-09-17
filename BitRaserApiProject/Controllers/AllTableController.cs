using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BitRaserApiProject.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MachinesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MachinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get all machines
        [HttpGet]
        public async Task<ActionResult<IEnumerable<machines>>> GetMachines()
        {
            return await _context.Machines.ToListAsync();
        }

        // ✅ Get a machine by its fingerprint hash (Primary Key)
        [HttpGet("by-hash/{hash}")]
        public async Task<ActionResult<machines>> GetMachineByHash(string hash)
        {
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.fingerprint_hash == hash);
            return machine == null ? NotFound() : Ok(machine);
        }

        [AllowAnonymous]
        // ✅ Get a machine by MAC Address
        [HttpGet("by-mac/{mac}")]
        public async Task<ActionResult<machines>> GetMachineByMac(string mac)
        {
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == mac);
            return machine == null ? NotFound() : Ok(machine);
        }

        // ✅ Get machines by user email
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<machines>>> GetMachinesByEmail(string email)
        {
            var machines = await _context.Machines.Where(m => m.user_email == email).ToListAsync();
            return machines.Any() ? Ok(machines) : NotFound();
        }

        //// ✅ Get license status by MAC Address
        //[HttpGet("license-status/{mac}")]
        //public async Task<IActionResult> GetLicenseStatus(string mac)
        //{
        //    var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == mac);
        //    if (machine == null) return NotFound(new { message = "Machine not found" });

        //    bool isActivated = machine.license_activated;
        //    bool isExpired = isActivated && DateTime.UtcNow > machine.license_activation_date?.AddDays(machine.license_days_valid);

        //    return Ok(new
        //    {
        //        machine.mac_address,
        //        isActivated,
        //        isExpired,
        //        ActivationDate = machine.license_activation_date
        //    });
        //}

        //// ✅ Get remaining license days for an activated machine
        //[HttpGet("remaining-days/{mac}")]
        //public async Task<IActionResult> GetRemainingDays(string mac)
        //{
        //    var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == mac);
        //    if (machine == null || !machine.license_activated)
        //        return NotFound(new { message = "Machine not found or not activated" });

        //    int remainingDays = Math.Max(0, (machine.license_activation_date?.AddDays(machine.license_days_valid) - DateTime.UtcNow)?.Days ?? 0);

        //    return Ok(new { machine.mac_address, remainingDays });
        //}


        // ✅ Create a machine entry using MAC, Email, CPU ID, or BIOS Serial
        [HttpPost]
        public async Task<ActionResult<machines>> CreateMachine([FromBody] machines machine)
        {
            if (string.IsNullOrWhiteSpace(machine.mac_address) || string.IsNullOrWhiteSpace(machine.fingerprint_hash))
                return BadRequest(new { message = "MAC Address and Fingerprint Hash are required" });

            _context.Machines.Add(machine);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMachineByHash), new { hash = machine.fingerprint_hash }, machine);
        }

        // ✅ Update a machine's details
        [HttpPut("{hash}")]
        public async Task<IActionResult> UpdateMachine(string hash, [FromBody] machines updatedMachine)
        {
            if (hash != updatedMachine.fingerprint_hash)
                return BadRequest(new { message = "Fingerprint hash mismatch" });

            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.fingerprint_hash == hash);
            if (machine == null) return NotFound();

            // Update allowed fields
            machine.mac_address = updatedMachine.mac_address;
            machine.cpu_id = updatedMachine.cpu_id;
            machine.bios_serial = updatedMachine.bios_serial;
            machine.os_version = updatedMachine.os_version;
            machine.user_email = updatedMachine.user_email;
            //machine.license_activated = updatedMachine.license_activated;
            machine.license_activation_date = updatedMachine.license_activation_date;
            machine.license_days_valid = updatedMachine.license_days_valid;

            _context.Entry(machine).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ Delete a machine entry
        [HttpDelete("{hash}")]
        public async Task<IActionResult> DeleteMachine(string hash)
        {
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.fingerprint_hash == hash);
            if (machine == null) return NotFound();

            _context.Machines.Remove(machine);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ Renew a machine's license by adding additional days
        [HttpPatch("renew-license/{mac}")]
        public async Task<IActionResult> RenewLicense(string mac, [FromQuery] int additionalDays)
        {
            if (additionalDays <= 0)
                return BadRequest(new { message = "Additional days must be greater than zero" });

            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == mac);
            if (machine == null) return NotFound(new { message = "Machine not found" });

            machine.license_days_valid += additionalDays;
            _context.Entry(machine).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"License extended by {additionalDays} days", machine.license_days_valid });
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all reports
        [HttpGet]
        public async Task<ActionResult<IEnumerable<audit_reports>>> GetAuditReports()
        {
            return await _context.AuditReports.ToListAsync();
        }

        // Get single report by id
        [HttpGet("{id}")]
        public async Task<ActionResult<audit_reports>> GetAuditReport(int id)
        {
            var report = await _context.AuditReports.FindAsync(id);
            return report == null ? NotFound() : Ok(report);
        }

        // Get all reports by client email
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<audit_reports>>> GetAuditReportsByEmail(string email)
        {
            var reports = await _context.AuditReports.Where(r => r.client_email == email).ToListAsync();
            return reports.Any() ? Ok(reports) : NotFound();
        }

        // Create a new report (full data) — anonymous allowed for flexibility
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<audit_reports>> CreateAuditReport([FromBody] audit_reports report)
        {
            _context.AuditReports.Add(report);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAuditReport), new { id = report.report_id }, report);
        }

        // Update full report data by id (except synced)
        [AllowAnonymous]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuditReport(int id, [FromBody] audit_reports updatedReport)
        {
            if (id != updatedReport.report_id)
                return BadRequest(new { message = "Report ID mismatch" });

            var report = await _context.AuditReports.FindAsync(id);
            if (report == null) return NotFound();

            report.report_name = updatedReport.report_name;
            report.erasure_method = updatedReport.erasure_method;
            report.report_details_json = updatedReport.report_details_json;

            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Delete report by id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditReport(int id)
        {
            var report = await _context.AuditReports.FindAsync(id);
            if (report == null) return NotFound();

            _context.AuditReports.Remove(report);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- New endpoints for your unique report ID flow ---
        [AllowAnonymous]
        // Reserve unique report ID (creates stub report with minimal data, synced = false)
        [HttpPost("reserve-id")]
        public async Task<ActionResult<int>> ReserveReportId([FromBody] string clientEmail)
        {
            var newReport = new audit_reports
            {
                client_email = clientEmail,
                synced = false,
                report_details_json = "{}",
                report_name = "Reserved",
                erasure_method = "Reserved"
            };

            _context.AuditReports.Add(newReport);
            await _context.SaveChangesAsync();

            return Ok(newReport.report_id);
        }

        [AllowAnonymous]
        // Upload full report data after reserving ID (except synced)
        [HttpPut("upload-report/{id}")]
        public async Task<IActionResult> UploadReportData(int id, [FromBody] audit_reports updatedReport)
        {
            if (id != updatedReport.report_id)
                return BadRequest(new { message = "Report ID mismatch" });

            var report = await _context.AuditReports.FindAsync(id);
            if (report == null)
                return NotFound();

            report.report_name = updatedReport.report_name;
            report.erasure_method = updatedReport.erasure_method;
            report.report_details_json = updatedReport.report_details_json;

            // synced flag remains unchanged here

            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [AllowAnonymous]
        // Mark report as synced after full upload
        [HttpPatch("mark-synced/{id}")]
        public async Task<IActionResult> MarkReportSynced(int id)
        {
            var report = await _context.AuditReports.FindAsync(id);
            if (report == null)
                return NotFound();

            report.synced = true;
            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }


    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<users>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<users>> GetUserByEmail(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            return user == null ? NotFound() : Ok(user);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<users>> CreateUser([FromBody] users user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUserByEmail), new { email = user.user_email }, user);
        }

        [HttpPut("{email}")]
        public async Task<IActionResult> UpdateUser(string email, [FromBody] users updatedUser)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null)
                return NotFound();

            user.user_name = updatedUser.user_name;
            user.phone_number = updatedUser.phone_number;
            user.user_email = updatedUser.user_email;

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("update-license/{email}")]
        public async Task<IActionResult> UpdateUserLicense(string email, [FromBody] string licenseJson)
        {
            var decodedEmail = Uri.UnescapeDataString(email); // Properly decode %40 back to @
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == decodedEmail);
            if (user == null) return NotFound();

            user.license_details_json = licenseJson;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("update-payment/{email}")]
        public async Task<IActionResult> UpdatePaymentDetails(string email, [FromBody] string paymentJson)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null)
                return NotFound();

            user.payment_details_json = paymentJson;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("change-password/{email}")]
        public async Task<IActionResult> ChangePassword(string email, [FromBody] string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null)
                return NotFound();

            user.user_password = newPassword; // Reminder: hash passwords in production
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

   
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : ControllerBase
    {
        [HttpGet("server-time")]
        public IActionResult GetServerTime()
        {
            var serverTimeUtc = DateTime.UtcNow;
            return Ok(new { server_time = serverTimeUtc.ToString("o") }); // ISO 8601 format
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            if (!await IsValidUserAsync(login.Email, login.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            // Generate JWT token here (your existing token generation code)
            var token = GenerateJwtToken(login.Email);

            return Ok(new { token });
        }


        private async Task<bool> IsValidUserAsync(string email, string password)
        {
            // Query the user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.user_email == email);

            if (user == null)
                return false;

            // WARNING: In production, you MUST store hashed passwords and verify hash here.
            // For now, assuming passwords are stored as plain text (not recommended).
            return user.user_password == password;
        }

        private string GenerateJwtToken(string username)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

    [Route("api/[controller]")]
    [ApiController]
    public class UpdatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UpdatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/updates/latest
        [HttpGet("latest")]
        public async Task<ActionResult<Update>> GetLatestVersion()
        {
            var latest = await _context.Updates
                .OrderByDescending(u => u.version_id)
                .FirstOrDefaultAsync();

            if (latest == null)
                return NotFound("No update records found.");

            return Ok(latest);
        }

        // GET: api/updates/check/{currentVersionId}
        // Returns all versions with version_id > currentVersionId
        [HttpGet("check/{currentVersionId}")]
        public async Task<ActionResult<IEnumerable<Update>>> CheckForUpdates(int currentVersionId)
        {
            var updates = await _context.Updates
                .Where(u => u.version_id > currentVersionId)
                .OrderBy(u => u.version_id)
                .ToListAsync();

            if (updates.Count == 0)
                return NoContent(); // No updates available

            return Ok(updates);
        }
    }
}


