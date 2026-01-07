using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using System.Data;
using DSecureApi.Models;
using System.Reflection.PortableExecutable;

namespace DSecureApi.Services
{
 /// <summary>
    /// High-performance Dapper service for hierarchical queries
    /// Implements parent-child relationship: User → Subusers → Resources
  /// </summary>
    public interface IDapperService
    {
        Task<IEnumerable<machines>> GetMachinesByUserEmailAsync(string userEmail);
      Task<IEnumerable<AuditReport>> GetAuditReportsByUserEmailAsync(string userEmail);
      Task<IEnumerable<Sessions>> GetSessionsByUserEmailAsync(string userEmail);
        Task<IEnumerable<logs>> GetLogsByUserEmailAsync(string userEmail);
        Task<IEnumerable<Commands>> GetCommandsByUserEmailAsync(string userEmail);
    Task<IEnumerable<subuser>> GetSubusersByUserEmailAsync(string userEmail);
        Task<UserResourcesSummary> GetUserResourcesSummaryAsync(string userEmail);
    }

    public class DapperService : IDapperService
    {
        private readonly string _connectionString;
    private readonly ILogger<DapperService> _logger;

        public DapperService(IConfiguration configuration, ILogger<DapperService> logger)
        {
       // ✅ FIX: Use same connection string name as Program.cs
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ApplicationDbContextConnection")
                ?? configuration.GetConnectionString("ApplicationDbContextConnection")
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string 'ApplicationDbContextConnection' or 'DefaultConnection' not found");
            
   _logger = logger;
        }

        private IDbConnection GetConnection()
        {
        // Use MySqlConnector for MySQL
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Get machines by user email (own + subusers' machines)
        /// ✅ High Performance with Dapper
    /// </summary>
   public async Task<IEnumerable<machines>> GetMachinesByUserEmailAsync(string userEmail)
   {
      using var connection = GetConnection();
    
const string sql = @"
      SELECT m.* 
   FROM machines m
         WHERE m.user_email = @UserEmail
  OR m.subuser_email IN (
      SELECT subuser_email 
                 FROM subuser 
    WHERE user_email = @UserEmail
        )
        ORDER BY m.created_at DESC";

            try
            {
                var result = await connection.QueryAsync<machines>(sql, new { UserEmail = userEmail });
 _logger.LogInformation("Retrieved {Count} machines for user {UserEmail}", result.Count(), userEmail);
     return result;
            }
            catch (Exception ex)
        {
     _logger.LogError(ex, "Error getting machines for user {UserEmail}", userEmail);
       throw;
            }
        }

  /// <summary>
      /// Get audit reports by user email (own + subusers' reports)
        /// ✅ High Performance with Dapper
  /// </summary>
  public async Task<IEnumerable<AuditReport>> GetAuditReportsByUserEmailAsync(string userEmail)
        {
            using var connection = GetConnection();
            
            const string sql = @"
        SELECT ar.* 
              FROM AuditReports ar
    WHERE ar.client_email = @UserEmail
         OR ar.client_email IN (
            SELECT subuser_email 
                FROM subuser 
     WHERE user_email = @UserEmail
      )
    ORDER BY ar.report_datetime DESC";

      try
            {
   var result = await connection.QueryAsync<AuditReport>(sql, new { UserEmail = userEmail });
            _logger.LogInformation("Retrieved {Count} audit reports for user {UserEmail}", result.Count(), userEmail);
    return result;
          }
      catch (Exception ex)
   {
      _logger.LogError(ex, "Error getting audit reports for user {UserEmail}", userEmail);
                throw;
      }
        }

  /// <summary>
      /// Get sessions by user email (own + subusers' sessions)
        /// ✅ High Performance with Dapper
    /// </summary>
      public async Task<IEnumerable<Sessions>> GetSessionsByUserEmailAsync(string userEmail)
        {
 using var connection = GetConnection();
            
    const string sql = @"
      SELECT s.* 
                FROM Sessions s
      WHERE s.user_email = @UserEmail
  OR s.user_email IN (
    SELECT subuser_email 
        FROM subuser 
      WHERE user_email = @UserEmail
          )
                ORDER BY s.login_time DESC";

            try
            {
  var result = await connection.QueryAsync<Sessions>(sql, new { UserEmail = userEmail });
            _logger.LogInformation("Retrieved {Count} sessions for user {UserEmail}", result.Count(), userEmail);
  return result;
            }
 catch (Exception ex)
            {
   _logger.LogError(ex, "Error getting sessions for user {UserEmail}", userEmail);
       throw;
  }
        }

        /// <summary>
        /// Get logs by user email (own + subusers' logs)
        /// ✅ High Performance with Dapper
        /// </summary>
        public async Task<IEnumerable<logs>> GetLogsByUserEmailAsync(string userEmail)
{
            using var connection = GetConnection();
            
            const string sql = @"
         SELECT l.* 
  FROM logs l
     WHERE l.user_email = @UserEmail
          OR l.user_email IN (
         SELECT subuser_email 
      FROM subuser 
  WHERE user_email = @UserEmail
        )
  ORDER BY l.created_at DESC";

    try
    {
          var result = await connection.QueryAsync<logs>(sql, new { UserEmail = userEmail });
    _logger.LogInformation("Retrieved {Count} logs for user {UserEmail}", result.Count(), userEmail);
             return result;
    }
    catch (Exception ex)
{
     _logger.LogError(ex, "Error getting logs for user {UserEmail}", userEmail);
   throw;
        }
        }

        /// <summary>
    /// Get commands by user email (own + subusers' commands from JSON)
        /// ✅ High Performance with Dapper
        /// </summary>
        public async Task<IEnumerable<Commands>> GetCommandsByUserEmailAsync(string userEmail)
    {
            using var connection = GetConnection();
     
  // First get all commands, then filter in memory (MySQL JSON limitation)
            const string sql = @"
          SELECT * 
            FROM Commands
        ORDER BY command_id  DESC";

       try
            {
var allCommands = await connection.QueryAsync<Commands>(sql);
      
   // Get subuser emails
        var subuserEmails = await GetSubuserEmailsAsync(userEmail);
         var allEmails = new List<string> { userEmail };
     allEmails.AddRange(subuserEmails);

        // Filter by user_email in command_json (client-side)
             var filtered = allCommands.Where(c => 
       {
        if (string.IsNullOrEmpty(c.command_json) || c.command_json == "{}")
   return false;
        
    var jsonStr = c.command_json.ToLower();  // ✅ FIX: Store as string variable
     return allEmails.Any(email => 
              jsonStr.Contains($"\"user_email\":\"{email.ToLower()}\"") ||
   jsonStr.Contains($"\"issued_by\":\"{email.ToLower()}\"")
             );
      }).ToList();

       _logger.LogInformation("Retrieved {Count} commands for user {UserEmail}", filtered.Count, userEmail);
       return filtered;
            }
  catch (Exception ex)
     {
              _logger.LogError(ex, "Error getting commands for user {UserEmail}", userEmail);
        throw;
         }
        }

        /// <summary>
 /// Get subusers by user email
        /// </summary>
        public async Task<IEnumerable<subuser>> GetSubusersByUserEmailAsync(string userEmail)
        {
            using var connection = GetConnection();
 
            const string sql = @"
   SELECT * 
         FROM subuser
  WHERE user_email = @UserEmail
 ORDER BY subuser_email";

  try
       {
   var result = await connection.QueryAsync<subuser>(sql, new { UserEmail = userEmail });
        _logger.LogInformation("Retrieved {Count} subusers for user {UserEmail}", result.Count(), userEmail);
          return result;
}
        catch (Exception ex)
            {
             _logger.LogError(ex, "Error getting subusers for user {UserEmail}", userEmail);
                throw;
            }
        }

  /// <summary>
        /// Get comprehensive summary of all user resources
        /// ✅ TiDB Compatible - Separate queries instead of multi-query
        /// </summary>
  public async Task<UserResourcesSummary> GetUserResourcesSummaryAsync(string userEmail)
        {
    using var connection = GetConnection();

            try
            {
     // Step 1: Get all subusers
        const string subuserSql = "SELECT * FROM subuser WHERE user_email = @UserEmail ORDER BY subuser_email";
   var subusers = (await connection.QueryAsync<subuser>(subuserSql, new { UserEmail = userEmail })).ToList();
      
        // Step 2: Get user's OWN resources (not subusers')
      const string ownMachinesSql = "SELECT * FROM machines WHERE user_email = @UserEmail AND (subuser_email IS NULL OR subuser_email = '') ORDER BY created_at DESC";
         var ownMachines = (await connection.QueryAsync<machines>(ownMachinesSql, new { UserEmail = userEmail })).ToList();
   
       const string ownReportsSql = "SELECT * FROM AuditReports WHERE client_email = @UserEmail ORDER BY report_id DESC";
 var ownReports = (await connection.QueryAsync<AuditReport>(ownReportsSql, new { UserEmail = userEmail })).ToList();
      
         const string ownSessionsSql = "SELECT * FROM Sessions WHERE user_email = @UserEmail ORDER BY login_time DESC";
        var ownSessions = (await connection.QueryAsync<Sessions>(ownSessionsSql, new { UserEmail = userEmail })).ToList();
        
      const string ownLogsSql = "SELECT * FROM logs WHERE user_email = @UserEmail ORDER BY created_at DESC";
                var ownLogs = (await connection.QueryAsync<logs>(ownLogsSql, new { UserEmail = userEmail })).ToList();

// Step 3: Get user's OWN commands (from JSON)
  var ownCommands = await GetCommandsByUserEmailAsync(userEmail);
        var ownCommandsList = ownCommands.Where(c => 
  {
      var jsonStr = c.command_json?.ToLower() ?? "";
     return jsonStr.Contains($"\"user_email\":\"{userEmail.ToLower()}\"") &&
               !subusers.Any(s => jsonStr.Contains($"\"user_email\":\"{s.subuser_email.ToLower()}\""));
           }).ToList();

      // Step 4: Get resources for EACH subuser (nested level)
    var subusersWithResources = new List<SubuserWithResources>();
   
         foreach (var subuser in subusers)
                {
   // Machines for this subuser
          const string subuserMachinesSql = @"
            SELECT * FROM machines 
     WHERE subuser_email = @SubuserEmail 
         ORDER BY created_at DESC";
            var subuserMachines = (await connection.QueryAsync<machines>(
 subuserMachinesSql, 
       new { SubuserEmail = subuser.subuser_email })).ToList();
        
// Reports for this subuser
          const string subuserReportsSql = @"
  SELECT * FROM AuditReports 
      WHERE client_email = @SubuserEmail 
        ORDER BY report_id DESC";
       var subuserReports = (await connection.QueryAsync<AuditReport>(
               subuserReportsSql, 
   new { SubuserEmail = subuser.subuser_email })).ToList();
 
   // Sessions for this subuser
       const string subuserSessionsSql = @"
              SELECT * FROM Sessions 
     WHERE user_email = @SubuserEmail 
     ORDER BY login_time DESC";
   var subuserSessions = (await connection.QueryAsync<Sessions>(
           subuserSessionsSql, 
              new { SubuserEmail = subuser.subuser_email })).ToList();
  
      // Logs for this subuser
   const string subuserLogsSql = @"
      SELECT * FROM logs 
          WHERE user_email = @SubuserEmail 
                  ORDER BY created_at DESC";
      var subuserLogs = (await connection.QueryAsync<logs>(
     subuserLogsSql, 
   new { SubuserEmail = subuser.subuser_email })).ToList();
      
// Commands for this subuser (from JSON)
        var subuserCommands = await GetCommandsByUserEmailAsync(subuser.subuser_email);
      var subuserCommandsList = subuserCommands.ToList();

               // Create nested structure
            subusersWithResources.Add(new SubuserWithResources
     {
           SubuserInfo = subuser,
    Machines = subuserMachines,
            AuditReports = subuserReports,
     Sessions = subuserSessions,
          Logs = subuserLogs,
    Commands = subuserCommandsList,
     MachinesCount = subuserMachines.Count,
      AuditReportsCount = subuserReports.Count,
 SessionsCount = subuserSessions.Count,
  LogsCount = subuserLogs.Count,
  CommandsCount = subuserCommandsList.Count
         });
              }

 // Step 5: Calculate totals
        var totalMachines = ownMachines.Count + subusersWithResources.Sum(s => s.MachinesCount);
        var totalReports = ownReports.Count + subusersWithResources.Sum(s => s.AuditReportsCount);
       var totalSessions = ownSessions.Count + subusersWithResources.Sum(s => s.SessionsCount);
       var totalLogs = ownLogs.Count + subusersWithResources.Sum(s => s.LogsCount);
    var totalCommands = ownCommandsList.Count + subusersWithResources.Sum(s => s.CommandsCount);

       var summary = new UserResourcesSummary
      {
            UserEmail = userEmail,
      Subusers = subusersWithResources,
        OwnMachines = ownMachines,
   OwnAuditReports = ownReports,
      OwnSessions = ownSessions,
           OwnLogs = ownLogs,
      OwnCommands = ownCommandsList,
        TotalMachines = totalMachines,
 TotalAuditReports = totalReports,
        TotalSessions = totalSessions,
           TotalLogs = totalLogs,
                 TotalCommands = totalCommands
      };

        _logger.LogInformation(
     "Retrieved NESTED summary for user {UserEmail}: {SubuserCount} subusers, {TotalMachines} machines (own: {OwnMachines})", 
       userEmail, subusersWithResources.Count, totalMachines, ownMachines.Count);
       
      return summary;
       }
    catch (Exception ex)
          {
     _logger.LogError(ex, "Error getting nested resource summary for user {UserEmail}", userEmail);
     throw;
            }
   }

      /// <summary>
        /// Helper method to get subuser emails
        /// </summary>
      private async Task<List<string>> GetSubuserEmailsAsync(string userEmail)
        {
            using var connection = GetConnection();
     
       const string sql = "SELECT subuser_email FROM subuser WHERE user_email = @UserEmail";
            
            var result = await connection.QueryAsync<string>(sql, new { UserEmail = userEmail });
   return result.ToList();
        }
    }

    /// <summary>
    /// User resources summary model
    /// </summary>
    public class UserResourcesSummary
    {
        public string UserEmail { get; set; } = string.Empty;
        public List<SubuserWithResources> Subusers { get; set; } = new();
        
        // Own resources (not including subusers)
        public List<machines> OwnMachines { get; set; } = new();
        public List<AuditReport> OwnAuditReports { get; set; } = new();
        public List<Sessions> OwnSessions { get; set; } = new();
        public List<logs> OwnLogs { get; set; } = new();
        public List<Commands> OwnCommands { get; set; } = new();
     
        // Total counts (own + all subusers)
        public int TotalMachines { get; set; }
        public int TotalAuditReports { get; set; }
        public int TotalSessions { get; set; }
        public int TotalLogs { get; set; }
        public int TotalCommands { get; set; }
    }

    /// <summary>
    /// Subuser with their resources (nested level)
    /// </summary>
  public class SubuserWithResources
    {
   public subuser SubuserInfo { get; set; } = new();
 public List<machines> Machines { get; set; } = new();
        public List<AuditReport> AuditReports { get; set; } = new();
        public List<Sessions> Sessions { get; set; } = new();
        public List<logs> Logs { get; set; } = new();
     public List<Commands> Commands { get; set; } = new();
  
        // Summary counts for this subuser
    public int MachinesCount { get; set; }
    public int AuditReportsCount { get; set; }
        public int SessionsCount { get; set; }
        public int LogsCount { get; set; }
    public int CommandsCount { get; set; }
    }
}
