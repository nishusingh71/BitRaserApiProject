namespace DSecureApi.Models.DTOs
{
    public class License
    {
        public record ActivateRequest(string LicenseKey, string Hwid);
        public record ActivateResponse(string Status, string? Expiry, string? Edition, int? ServerRevision, string? LicenseStatus);
        public record RenewRequest(string LicenseKey);
        public record RenewResponse(string Status, string? NewExpiry, int? ServerRevision);
        public record UpgradeRequest(string LicenseKey, string NewEdition);
        public record UpgradeResponse(string Status, string? Edition, int? ServerRevision);
        public record SyncRequest(string LicenseKey, string Hwid, int LocalRevision);
        public record SyncResponse(string Status, string? Expiry, string? Edition, int? ServerRevision, string? LicenseStatus);
    }
}
