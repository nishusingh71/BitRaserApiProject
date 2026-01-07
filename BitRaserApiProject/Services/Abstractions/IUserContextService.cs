namespace DSecureApi.Services.Abstractions
{
    public interface IUserContextService
    {
        Task<UserContext?> GetUserContextAsync(string email);
        Task<bool> IsSubuserAsync(string email);
        void InvalidateUser(string email);
    }

    public class UserContext
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsSubuser { get; set; }
        public int? SubuserId { get; set; }
        public string? ParentEmail { get; set; }
        public bool PrivateApi { get; set; }
    }
}
