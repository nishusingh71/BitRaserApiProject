namespace DSecureApi.Services
{
    /// <summary>
    /// Service interface for resolving tenant-specific database connections
/// </summary>
    public interface ITenantConnectionService
    {
     /// <summary>
 /// Gets the connection string for the current user's dynamic database
      /// Returns Main DB connection if user is not private cloud
        /// Returns decrypted private connection if user is private cloud
        /// </summary>
     Task<string> GetConnectionStringAsync();

/// <summary>
        /// Gets the connection string for a specific user by email
        /// </summary>
        Task<string> GetConnectionStringForUserAsync(string userEmail);

        /// <summary>
     /// Gets the current authenticated user's email from claims
        /// </summary>
        string? GetCurrentUserEmail();

        /// <summary>
   /// Checks if current user has private cloud enabled
        /// </summary>
        Task<bool> IsPrivateCloudUserAsync();
    }
}
