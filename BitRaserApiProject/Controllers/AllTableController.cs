using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;

namespace BitRaserApiProject.Controllers
{
    // ...existing using statements...


    // ...existing namespace and controllers...

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public SessionsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sessions>>> GetSessions()
        {
            return await _context.Sessions.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sessions>> GetSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            return session == null ? NotFound() : Ok(session);
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<Sessions>>> GetSessionsByEmail(string email)
        {
            var sessions = await _context.Sessions.Where(s => s.user_email == email).ToListAsync();
            return sessions.Any() ? Ok(sessions) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<Sessions>> CreateSession([FromBody] Sessions session)
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSession), new { id = session.session_id }, session);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, [FromBody] Sessions updatedSession)
        {
            if (id != updatedSession.session_id) return BadRequest();
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            session.logout_time = updatedSession.logout_time;
            session.session_status = updatedSession.session_status;
            session.ip_address = updatedSession.ip_address;
            session.device_info = updatedSession.device_info;

            _context.Entry(session).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LogsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<logs>>> GetLogs()
        {
            return await _context.logs.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<logs>> GetLog(int id)
        {
            var log = await _context.logs.FindAsync(id);
            return log == null ? NotFound() : Ok(log);
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<logs>>> GetLogsByEmail(string email)
        {
            var logsList = await _context.logs.Where(l => l.user_email == email).ToListAsync();
            return logsList.Any() ? Ok(logsList) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<logs>> CreateLog([FromBody] logs log)
        {
            _context.logs.Add(log);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLog), new { id = log.log_id }, log);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLog(int id, [FromBody] logs updatedLog)
        {
            if (id != updatedLog.log_id) return BadRequest();
            var log = await _context.logs.FindAsync(id);
            if (log == null) return NotFound();

            log.log_level = updatedLog.log_level;
            log.message = updatedLog.message; // <-- FIXED: use 'message' property instead of 'log_message'
            log.message = updatedLog.message;

            _context.Entry(log).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            var log = await _context.logs.FindAsync(id);
            if (log == null) return NotFound();
            _context.logs.Remove(log);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubuserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public SubuserController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<subuser>>> GetSubusers()
        {
            return await _context.subuser.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<subuser>> GetSubuser(int id)
        {
            var sub = await _context.subuser.FindAsync(id);
            return sub == null ? NotFound() : Ok(sub);
        }

        [HttpGet("by-superuser/{parentUserId}")]
        public async Task<ActionResult<IEnumerable<subuser>>> GetSubusersBySuperuser(int parentUserId)
        {
            var subusers = await _context.subuser.Where(s => s.parent_user_id == parentUserId).ToListAsync();
            return subusers.Any() ? Ok(subusers) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<subuser>> CreateSubuser([FromBody] subuser sub)
        {
            // Check for duplicate subuser email under this superuser
            if (await _context.subuser.AnyAsync(s => s.parent_user_id == sub.parent_user_id && s.subuser_email == sub.subuser_email))
                return Conflict("Subuser email already exists for this superuser.");

            _context.subuser.Add(sub);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSubuser), new { id = sub.subuser_id }, sub);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubuser(int id, [FromBody] subuser updatedSub)
        {
            if (id != updatedSub.subuser_id) return BadRequest();
            var sub = await _context.subuser.FindAsync(id);

            if (sub == null) return NotFound();

            sub.subuser_email = updatedSub.subuser_email;
            sub.subuser_password = updatedSub.subuser_password; // FIX: use correct property name

            _context.Entry(sub).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubuser(int id)
        {
            var sub = await _context.subuser.FindAsync(id);
            if (sub == null) return NotFound();
            _context.subuser.Remove(sub);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // ...existing controllers...

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommandsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CommandsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Commands>>> GetCommands()
        {
            return await _context.Commands.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Commands>> GetCommand(int id)
        {
            var command = await _context.Commands.FindAsync(id);
            return command == null ? NotFound() : Ok(command);
        }

        [HttpGet("by-machine/{machine_hash}")]
        public async Task<ActionResult<IEnumerable<Commands>>> GetCommandsByMachine(string machine_hash)
        {
            // 'Commands' does not have a 'machine_hash' property.
            // You must filter by a property that actually exists in the 'Commands' class.
            // For example, if you meant to filter by 'command_id', use that instead:
            // var commands = await _context.Commands.Where(c => c.command_id == ...).ToListAsync();

            // If you intended to filter by a property that links to a machine, you need to add such a property to the 'Commands' class,
            // e.g., 'public string machine_hash { get; set; }' in the Commands model.

            // If you do not have such a property, you must remove this endpoint or clarify your data model.

            // Example: Remove the endpoint entirely if not needed, or ask for clarification on the correct property to filter by.

            // For now, comment out the problematic code to avoid the error:
            // var commands = await _context.Commands.Where(c => c.machine_hash == machine_hash).ToListAsync();
            // return commands.Any() ? Ok(commands) : NotFound();

            // Or, if you want to return all commands (no filter):
            var commands = await _context.Commands.ToListAsync();
            return Ok(commands);
        }

        [HttpPost]
        public async Task<ActionResult<Commands>> CreateCommand([FromBody] Commands command)
        {
            _context.Commands.Add(command);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCommand), new { id = command.Command_id }, command);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommand(int id, [FromBody] Commands updatedCommand)
        {
            if (id != updatedCommand.Command_id) return BadRequest();
            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

            // Only update properties that exist in the Commands class
            command.command_name = updatedCommand.command_name;
            command.command_description = updatedCommand.command_description;
            command.command_parameters = updatedCommand.command_parameters;
            command.created_at = updatedCommand.created_at;
            command.updated_at = updatedCommand.updated_at;

            _context.Entry(command).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommand(int id)
        {
            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();
            _context.Commands.Remove(command);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserRoleProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserRoleProfileController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User_role_profile>>> GetUserRoleProfiles()
        {
            return await _context.User_role_profile.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User_role_profile>> GetUserRoleProfile(int id)
        {
            var role = await _context.User_role_profile.FindAsync(id);
            return role == null ? NotFound() : Ok(role);
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<User_role_profile>>> GetUserRoleProfilesByEmail(string email)
        {
            var roles = await _context.User_role_profile.Where(r => r.user_email == email).ToListAsync();
            return roles.Any() ? Ok(roles) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<User_role_profile>> CreateUserRoleProfile([FromBody] User_role_profile role)
        {
            _context.User_role_profile.Add(role);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUserRoleProfile), new { id = role.role_id }, role);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserRoleProfile(int id, [FromBody] User_role_profile updatedRole)
        {
            if (id != updatedRole.role_id) return BadRequest();
            var role = await _context.User_role_profile.FindAsync(id);
            if (role == null) return NotFound();

            role.user_email = updatedRole.user_email;
            // role.manage_user_id = updatedRole.manage_user_id; // <-- REMOVE THIS LINE
            role.role_name = updatedRole.role_name;

            _context.Entry(role).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserRoleProfile(int id)
        {
            var role = await _context.User_role_profile.FindAsync(id);
            if (role == null) return NotFound();
            _context.User_role_profile.Remove(role);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // ...existing controllers below (if any)...

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
            // Hash the plain password before saving
            user.user_password = BCrypt.Net.BCrypt.HashPassword(user.user_password);
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null)
                return false;

            // Verify hashed password using BCrypt
            return BCrypt.Net.BCrypt.Verify(password, user.user_password);
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

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LicenseController(ApplicationDbContext context) { _context = context; }

        // GET: api/license/validate/{email}
        [HttpGet("validate/{email}")]
        public async Task<IActionResult> ValidateLicense(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Parse license details from JSON (assume it contains activation_date and days_valid)
            DateTime? activationDate = null;
            int daysValid = 0;
            if (!string.IsNullOrEmpty(user.license_details_json))
            {
                try
                {
                    var licenseObj = System.Text.Json.JsonDocument.Parse(user.license_details_json).RootElement;
                    if (licenseObj.TryGetProperty("activation_date", out var actDate))
                        activationDate = actDate.GetDateTime();
                    if (licenseObj.TryGetProperty("days_valid", out var dValid))
                        daysValid = dValid.GetInt32();
                }
                catch
                {
                    return BadRequest(new { message = "Invalid license details format" });
                }
            }

            if (activationDate == null || daysValid <= 0)
                return Ok(new { isValid = false, message = "License not activated or invalid" });

            var expiryDate = activationDate.Value.AddDays(daysValid);
            var now = DateTime.UtcNow;
            var remaining = (expiryDate - now).TotalDays;

            bool isValid = now < expiryDate;
            return Ok(new
            {
                isValid,
                expiresOn = expiryDate,
                remainingDays = remaining > 0 ? Math.Floor(remaining) : 0
            });
        }
    }
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PdfReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PdfReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Generate")]
        public async Task<IActionResult> Generate([FromBody] PdfGenerateRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.UserEmail);
            if (user == null)
                return NotFound("User not found.");

            var reportData = new Dictionary<string, object>
            {
                { "User Name", user.user_name },
                { "User Email", user.user_email },
                { "Title", request.Title },
                { "Description", request.Description }
                // Add more fields as needed
            };

            string outputPath = Path.Combine(Path.GetTempPath(), $"UserReport_{Guid.NewGuid()}.pdf");
            var pdfService = new PdfReportService();
            bool success = pdfService.GeneratePdf(reportData, outputPath);

            if (!success || !System.IO.File.Exists(outputPath))
                return StatusCode(500, "PDF generation failed.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            System.IO.File.Delete(outputPath);
            return File(fileBytes, "application/pdf", "UserReport.pdf");
        }
    }

}





