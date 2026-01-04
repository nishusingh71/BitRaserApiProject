namespace BitRaserApiProject.Services.Abstractions
{
    public interface IPrivateCloudConfigCache
    {
        Task<PrivateCloudConfig?> GetConfigAsync(string userEmail);
        void InvalidateConfig(string userEmail);
    }

    public class PrivateCloudConfig
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
