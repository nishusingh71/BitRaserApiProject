using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// System Settings Controller - Complete system configuration management
    /// Matches BitRaser System Settings UI (General, Security, Notifications, License)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemSettingsController : ControllerBase
{
  private readonly ApplicationDbContext _context;
  private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
      private readonly ILogger<SystemSettingsController> _logger;

        public SystemSettingsController(
            ApplicationDbContext context,
 IRoleBasedAuthService authService,
            IUserDataService userDataService,
    ILogger<SystemSettingsController> logger)
  {
            _context = context;
      _authService = authService;
         _userDataService = userDataService;
      _logger = logger;
        }

     /// <summary>
        /// GET /api/SystemSettings - Get all system settings
  /// </summary>
        [HttpGet]
   public async Task<ActionResult<SystemSettingsResponseDto>> GetAllSettings()
        {
         try
  {
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(userEmail))
   {
  return Unauthorized(new { message = "User not authenticated" });
   }

       var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

       // Check permissions - only admins can view system settings
           if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser) &&
          !await _authService.HasPermissionAsync(userEmail, "SYSTEM_SETTINGS", isSubuser))
       {
            return StatusCode(403, new { message = "Insufficient permissions to view system settings" });
       }

    var response = new SystemSettingsResponseDto
    {
          General = await GetGeneralSettings(),
   Security = await GetSecuritySettings(),
           Notifications = await GetNotificationSettings(),
      License = await GetLicenseSettings()
         };

 return Ok(response);
  }
       catch (Exception ex)
            {
   _logger.LogError(ex, "Error retrieving system settings");
       return StatusCode(500, new { message = "Error retrieving system settings", error = ex.Message });
          }
        }

        /// <summary>
   /// GET /api/SystemSettings/general - Get general settings
 /// </summary>
        [HttpGet("general")]
        public async Task<ActionResult<GeneralSettingsDto>> GetGeneral()
    {
   try
       {
       var settings = await GetGeneralSettings();
           return Ok(settings);
            }
      catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving general settings");
           return StatusCode(500, new { message = "Error retrieving general settings" });
     }
 }

        /// <summary>
  /// PUT /api/SystemSettings/general - Update general settings
      /// </summary>
   [HttpPut("general")]
     public async Task<IActionResult> UpdateGeneral([FromBody] UpdateGeneralSettingsRequest request)
        {
 try
   {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
     {
        return Unauthorized(new { message = "User not authenticated" });
          }

          var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

 if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser))
    {
          return StatusCode(403, new { message = "Insufficient permissions to update system settings" });
              }

 // Update settings
     if (!string.IsNullOrEmpty(request.SiteName))
         await UpdateSetting("General", "SiteName", request.SiteName, userEmail);

 if (!string.IsNullOrEmpty(request.SiteDescription))
        await UpdateSetting("General", "SiteDescription", request.SiteDescription, userEmail);

  if (!string.IsNullOrEmpty(request.DefaultLanguage))
                await UpdateSetting("General", "DefaultLanguage", request.DefaultLanguage, userEmail);

         if (!string.IsNullOrEmpty(request.Timezone))
      await UpdateSetting("General", "Timezone", request.Timezone, userEmail);

           if (request.EnableMaintenanceMode.HasValue)
        await UpdateSetting("General", "EnableMaintenanceMode", request.EnableMaintenanceMode.Value.ToString(), userEmail);

      await _context.SaveChangesAsync();

   _logger.LogInformation("General settings updated by {Email}", userEmail);

         return Ok(new { message = "General settings updated successfully" });
    }
       catch (Exception ex)
            {
  _logger.LogError(ex, "Error updating general settings");
                return StatusCode(500, new { message = "Error updating general settings", error = ex.Message });
    }
  }

    /// <summary>
        /// GET /api/SystemSettings/security - Get security settings
        /// </summary>
        [HttpGet("security")]
   public async Task<ActionResult<SecuritySettingsDto>> GetSecurity()
   {
       try
   {
          var settings = await GetSecuritySettings();
             return Ok(settings);
        }
catch (Exception ex)
     {
          _logger.LogError(ex, "Error retrieving security settings");
return StatusCode(500, new { message = "Error retrieving security settings" });
    }
        }

        /// <summary>
        /// PUT /api/SystemSettings/security - Update security settings
        /// </summary>
     [HttpPut("security")]
   public async Task<IActionResult> UpdateSecurity([FromBody] UpdateSecuritySettingsRequest request)
     {
            try
            {
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   if (string.IsNullOrEmpty(userEmail))
           {
 return Unauthorized(new { message = "User not authenticated" });
       }

var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

     if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser) &&
     !await _authService.HasPermissionAsync(userEmail, "SECURITY_MANAGEMENT", isSubuser))
          {
      return StatusCode(403, new { message = "Insufficient permissions to update security settings" });
        }

                // Update settings
         if (request.PasswordMinimumLength.HasValue)
         await UpdateSetting("Security", "PasswordMinimumLength", request.PasswordMinimumLength.Value.ToString(), userEmail);

    if (request.SessionTimeoutMinutes.HasValue)
      await UpdateSetting("Security", "SessionTimeoutMinutes", request.SessionTimeoutMinutes.Value.ToString(), userEmail);

          if (request.MaxLoginAttempts.HasValue)
await UpdateSetting("Security", "MaxLoginAttempts", request.MaxLoginAttempts.Value.ToString(), userEmail);

         if (request.RequireSpecialCharactersInPasswords.HasValue)
await UpdateSetting("Security", "RequireSpecialCharactersInPasswords", request.RequireSpecialCharactersInPasswords.Value.ToString(), userEmail);

  if (request.EnableTwoFactorAuthentication.HasValue)
          await UpdateSetting("Security", "EnableTwoFactorAuthentication", request.EnableTwoFactorAuthentication.Value.ToString(), userEmail);

  await _context.SaveChangesAsync();

          _logger.LogInformation("Security settings updated by {Email}", userEmail);

   return Ok(new { message = "Security settings updated successfully" });
            }
  catch (Exception ex)
       {
     _logger.LogError(ex, "Error updating security settings");
    return StatusCode(500, new { message = "Error updating security settings", error = ex.Message });
            }
        }

        /// <summary>
 /// GET /api/SystemSettings/notifications - Get notification settings
        /// </summary>
        [HttpGet("notifications")]
  public async Task<ActionResult<NotificationSettingsDto>> GetNotifications()
        {
       try
            {
    var settings = await GetNotificationSettings();
   return Ok(settings);
 }
        catch (Exception ex)
       {
       _logger.LogError(ex, "Error retrieving notification settings");
        return StatusCode(500, new { message = "Error retrieving notification settings" });
   }
        }

        /// <summary>
      /// PUT /api/SystemSettings/notifications - Update notification settings
  /// </summary>
[HttpPut("notifications")]
        public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationSettingsRequest request)
      {
   try
            {
  var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
         if (string.IsNullOrEmpty(userEmail))
      {
         return Unauthorized(new { message = "User not authenticated" });
       }

            var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

  if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser))
         {
        return StatusCode(403, new { message = "Insufficient permissions to update notification settings" });
      }

          // Update settings
     if (request.EnableEmailNotifications.HasValue)
             await UpdateSetting("Notifications", "EnableEmailNotifications", request.EnableEmailNotifications.Value.ToString(), userEmail);

     if (request.NotifyOnNewUser.HasValue)
   await UpdateSetting("Notifications", "NotifyOnNewUser", request.NotifyOnNewUser.Value.ToString(), userEmail);

                if (request.NotifyOnLicenseExpiry.HasValue)
        await UpdateSetting("Notifications", "NotifyOnLicenseExpiry", request.NotifyOnLicenseExpiry.Value.ToString(), userEmail);

             if (request.NotifyOnSystemErrors.HasValue)
      await UpdateSetting("Notifications", "NotifyOnSystemErrors", request.NotifyOnSystemErrors.Value.ToString(), userEmail);

         if (!string.IsNullOrEmpty(request.AdminNotificationEmail))
 await UpdateSetting("Notifications", "AdminNotificationEmail", request.AdminNotificationEmail, userEmail);

    // New notification settings from Screenshot 3
     if (request.EnableSmsNotifications.HasValue)
   await UpdateSetting("Notifications", "EnableSmsNotifications", request.EnableSmsNotifications.Value.ToString(), userEmail);

       if (request.NotifyOnReportGeneration.HasValue)
  await UpdateSetting("Notifications", "NotifyOnReportGeneration", request.NotifyOnReportGeneration.Value.ToString(), userEmail);

           if (request.EnableSystemAlerts.HasValue)
    await UpdateSetting("Notifications", "EnableSystemAlerts", request.EnableSystemAlerts.Value.ToString(), userEmail);

   if (request.NotifyOnUserRegistration.HasValue)
            await UpdateSetting("Notifications", "NotifyOnUserRegistration", request.NotifyOnUserRegistration.Value.ToString(), userEmail);

        await _context.SaveChangesAsync();

  _logger.LogInformation("Notification settings updated by {Email}", userEmail);

       return Ok(new { message = "Notification settings updated successfully" });
      }
   catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating notification settings");
             return StatusCode(500, new { message = "Error updating notification settings", error = ex.Message });
         }
        }

        /// <summary>
        /// GET /api/SystemSettings/license - Get license settings (From Screenshot 1)
        /// </summary>
        [HttpGet("license")]
        public async Task<ActionResult<LicenseSettingsDto>> GetLicense()
        {
     try
     {
        var settings = await GetLicenseSettings();
       return Ok(settings);
    }
  catch (Exception ex)
    {
    _logger.LogError(ex, "Error retrieving license settings");
      return StatusCode(500, new { message = "Error retrieving license settings" });
    }
        }

        /// <summary>
   /// PUT /api/SystemSettings/license/auto-renew - Update auto-renewal setting
        /// </summary>
        [HttpPut("license/auto-renew")]
        public async Task<IActionResult> UpdateAutoRenew([FromBody] UpdateAutoRenewRequest request)
        {
       try
         {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
            {
            return Unauthorized(new { message = "User not authenticated" });
                }

       var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

         if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser))
       {
          return StatusCode(403, new { message = "Insufficient permissions to update license settings" });
 }

     await UpdateSetting("License", "AutoRenew", request.AutoRenew.ToString(), userEmail);
         await _context.SaveChangesAsync();

      _logger.LogInformation("License auto-renew setting updated to {Value} by {Email}", request.AutoRenew, userEmail);

    return Ok(new { message = "Auto-renewal setting updated successfully", autoRenew = request.AutoRenew });
      }
      catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auto-renewal setting");
    return StatusCode(500, new { message = "Error updating setting", error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/SystemSettings/license/expiry-date - Update license expiry date
        /// </summary>
        [HttpPut("license/expiry-date")]
 public async Task<IActionResult> UpdateLicenseExpiryDate([FromBody] UpdateLicenseExpiryRequest request)
        {
 try
    {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
              {
       return Unauthorized(new { message = "User not authenticated" });
        }

         var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser))
           {
    return StatusCode(403, new { message = "Insufficient permissions to update license settings" });
     }

                await UpdateSetting("License", "LicenseExpiryDate", request.ExpiryDate.ToString("yyyy-MM-dd"), userEmail);
      await _context.SaveChangesAsync();

       _logger.LogInformation("License expiry date updated to {Date} by {Email}", request.ExpiryDate, userEmail);

          return Ok(new { message = "License expiry date updated successfully", expiryDate = request.ExpiryDate });
            }
   catch (Exception ex)
      {
 _logger.LogError(ex, "Error updating license expiry date");
      return StatusCode(500, new { message = "Error updating expiry date", error = ex.Message });
      }
    }

        private async Task<NotificationSettingsDto> GetNotificationSettings()
   {
     return new NotificationSettingsDto
     {
       EnableEmailNotifications = bool.Parse(await GetSettingValue("Notifications", "EnableEmailNotifications", "true")),
        NotifyOnNewUser = bool.Parse(await GetSettingValue("Notifications", "NotifyOnNewUser", "true")),
   NotifyOnLicenseExpiry = bool.Parse(await GetSettingValue("Notifications", "NotifyOnLicenseExpiry", "true")),
       NotifyOnSystemErrors = bool.Parse(await GetSettingValue("Notifications", "NotifyOnSystemErrors", "true")),
      NotifyOnSecurityEvents = bool.Parse(await GetSettingValue("Notifications", "NotifyOnSecurityEvents", "true")),
   AdminNotificationEmail = await GetSettingValue("Notifications", "AdminNotificationEmail", ""),
         // New settings from Screenshot 3
  EnableSmsNotifications = bool.Parse(await GetSettingValue("Notifications", "EnableSmsNotifications", "false")),
    NotifyOnReportGeneration = bool.Parse(await GetSettingValue("Notifications", "NotifyOnReportGeneration", "true")),
 EnableSystemAlerts = bool.Parse(await GetSettingValue("Notifications", "EnableSystemAlerts", "true")),
       NotifyOnUserRegistration = bool.Parse(await GetSettingValue("Notifications", "NotifyOnUserRegistration", "true")),
         UpdatedAt = DateTime.UtcNow
     };
     }

        private async Task<LicenseSettingsDto> GetLicenseSettings()
        {
 var totalLicenses = await _context.Machines.CountAsync(m => m.license_activated);
       var usedLicenses = totalLicenses;

 return new LicenseSettingsDto
     {
LicenseType = await GetSettingValue("License", "LicenseType", "Enterprise"),
    TotalLicenses = int.Parse(await GetSettingValue("License", "TotalLicenses", totalLicenses.ToString())),
       UsedLicenses = usedLicenses,
         LicenseExpiryDate = DateTime.TryParse(await GetSettingValue("License", "LicenseExpiryDate", ""), out var expiryDate) 
? expiryDate 
     : null,
     AutoRenew = bool.Parse(await GetSettingValue("License", "AutoRenew", "false")),
        SendExpiryReminders = bool.Parse(await GetSettingValue("License", "SendExpiryReminders", "true")),
    ReminderDaysBeforeExpiry = int.Parse(await GetSettingValue("License", "ReminderDaysBeforeExpiry", "30")),
       UpdatedAt = DateTime.UtcNow
     };
 }

  #region Private Helper Methods

   private async Task<GeneralSettingsDto> GetGeneralSettings()
        {
     return new GeneralSettingsDto
  {
            SiteName = await GetSettingValue("General", "SiteName", "DSecureTech"),
                SiteDescription = await GetSettingValue("General", "SiteDescription", "Professional Data Erasure Solutions"),
 DefaultLanguage = await GetSettingValue("General", "DefaultLanguage", "English"),
         Timezone = await GetSettingValue("General", "Timezone", "UTC"),
EnableMaintenanceMode = bool.Parse(await GetSettingValue("General", "EnableMaintenanceMode", "false")),
 UpdatedAt = DateTime.UtcNow
 };
        }

  private async Task<SecuritySettingsDto> GetSecuritySettings()
 {
   return new SecuritySettingsDto
   {
     PasswordMinimumLength = int.Parse(await GetSettingValue("Security", "PasswordMinimumLength", "8")),
  SessionTimeoutMinutes = int.Parse(await GetSettingValue("Security", "SessionTimeoutMinutes", "30")),
     MaxLoginAttempts = int.Parse(await GetSettingValue("Security", "MaxLoginAttempts", "5")),
 RequireSpecialCharactersInPasswords = bool.Parse(await GetSettingValue("Security", "RequireSpecialCharactersInPasswords", "true")),
  EnableTwoFactorAuthentication = bool.Parse(await GetSettingValue("Security", "EnableTwoFactorAuthentication", "false")),
UpdatedAt = DateTime.UtcNow
  };
        }

        private async Task<string> GetSettingValue(string category, string key, string defaultValue)
     {
      var setting = await _context.Set<SystemSetting>()
 .Where(s => s.Category == category && s.SettingKey == key).FirstOrDefaultAsync();

  return setting?.SettingValue ?? defaultValue;
        }

        private async Task UpdateSetting(string category, string key, string value, string updatedBy)
     {
var setting = await _context.Set<SystemSetting>()
       .Where(s => s.Category == category && s.SettingKey == key).FirstOrDefaultAsync();

   if (setting == null)
       {
    setting = new SystemSetting
      {
 Category = category,
SettingKey = key,
  SettingValue = value,
     UpdatedBy = updatedBy,
          CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
          };
   _context.Set<SystemSetting>().Add(setting);
      }
        else
       {
         setting.SettingValue = value;
      setting.UpdatedBy = updatedBy;
        setting.UpdatedAt = DateTime.UtcNow;
       }
  }

   /// <summary>
        /// GET /api/SystemSettings/options - Get available options for dropdowns
        /// </summary>
        [HttpGet("options")]
        public ActionResult<SystemSettingsOptionsDto> GetOptions()
        {
     try
  {
    var options = new SystemSettingsOptionsDto
              {
       Languages = new List<LanguageOption>
      {
         new LanguageOption { Code = "en", Name = "English", NativeName = "English" },
       new LanguageOption { Code = "es", Name = "Spanish", NativeName = "Español" },
     new LanguageOption { Code = "fr", Name = "French", NativeName = "Français" },
         new LanguageOption { Code = "de", Name = "German", NativeName = "Deutsch" },
            new LanguageOption { Code = "zh", Name = "Chinese", NativeName = "中文" },
               new LanguageOption { Code = "ja", Name = "Japanese", NativeName = "日本語" }
},
  Timezones = TimeZoneInfo.GetSystemTimeZones()
      .Select(tz => new TimezoneOption
   {
       Id = tz.Id,
 DisplayName = tz.DisplayName,
      Offset = tz.BaseUtcOffset.ToString(@"hh\:mm")
             })
 .ToList(),
    DateFormats = new List<string> { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" },
     TimeFormats = new List<string> { "24h", "12h" }
 };

       return Ok(options);
       }
            catch (Exception ex)
            {
     _logger.LogError(ex, "Error retrieving settings options");
    return StatusCode(500, new { message = "Error retrieving options" });
       }
     }

        #endregion

        #region Billing Endpoints

        /// <summary>
        /// GET /api/SystemSettings/billing - Get billing information and subscription details
        /// </summary>
        [HttpGet("billing")]
        public async Task<IActionResult> GetBillingSettings()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                // Check permissions - only admins can view billing
                if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser) &&
                    !await _authService.HasPermissionAsync(userEmail, "BILLING_VIEW", isSubuser))
                {
                    return StatusCode(403, new { success = false, message = "Insufficient permissions to view billing" });
                }

                // Get license/subscription info
                var licenseSettings = await GetLicenseSettings();
                
                // Get usage counts
                var totalUsers = await _context.Users.CountAsync();
                var totalMachines = await _context.Machines.CountAsync();
                var activeLicenses = await _context.Machines.CountAsync(m => m.license_activated);
                var totalReports = await _context.AuditReports.CountAsync();

                // Get recent orders for payment history
                var recentOrders = await _context.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new
                    {
                        orderId = o.OrderId,
                        amountCents = o.AmountCents,
                        currency = o.Currency,
                        status = o.Status,
                        createdAt = o.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        // Subscription Details
                        subscription = new
                        {
                            plan = licenseSettings.LicenseType ?? "Enterprise",
                            status = "active",
                            renewalDate = licenseSettings.LicenseExpiryDate,
                            autoRenew = licenseSettings.AutoRenew,
                            daysUntilRenewal = licenseSettings.LicenseExpiryDate.HasValue
                                ? (int)(licenseSettings.LicenseExpiryDate.Value - DateTime.UtcNow).TotalDays
                                : 0
                        },
                        // Usage Summary
                        usage = new
                        {
                            users = new { current = totalUsers, limit = 1000 },
                            machines = new { current = totalMachines, limit = 5000 },
                            licenses = new { used = activeLicenses, total = licenseSettings.TotalLicenses },
                            reports = new { generated = totalReports, limit = 10000 },
                            storageUsedGb = 0,
                            storageLimit = 100
                        },
                        // Billing Contact
                        billingContact = new
                        {
                            email = await GetSettingValue("Billing", "ContactEmail", userEmail),
                            companyName = await GetSettingValue("Billing", "CompanyName", ""),
                            address = await GetSettingValue("Billing", "Address", "")
                        },
                        // Payment History
                        recentPayments = recentOrders,
                        generatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting billing settings");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/SystemSettings/billing/renew - Request subscription renewal
        /// </summary>
        [HttpPost("billing/renew")]
        public async Task<IActionResult> RequestRenewal([FromBody] RenewalRequest? request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                if (!await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser))
                {
                    return StatusCode(403, new { success = false, message = "Insufficient permissions" });
                }

                // Log renewal request
                _logger.LogInformation("🔄 Subscription renewal requested by {Email}, Plan: {Plan}", 
                    userEmail, request?.PlanType ?? "Current");

                return Ok(new
                {
                    success = true,
                    message = "Renewal request submitted. You will be redirected to payment.",
                    data = new
                    {
                        requestId = Guid.NewGuid().ToString("N"),
                        planType = request?.PlanType ?? "Enterprise",
                        requestedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error requesting renewal");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

    /// <summary>
    /// Renewal request DTO for billing/renew endpoint
    /// </summary>
    public class RenewalRequest
    {
        public string? PlanType { get; set; }
        public int? Months { get; set; } = 12;
    }
}
