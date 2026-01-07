namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Service type enum for product categorization
    /// Used for conditional email attachment rules
    /// </summary>
    public enum ServiceType
    {
        /// <summary>
        /// Unknown or unspecified service type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Drive Eraser service - Emails must include PDF only
        /// </summary>
        DriveEraser = 1,

        /// <summary>
        /// File Eraser service - Emails must include Excel only
        /// </summary>
        FileEraser = 2,

        /// <summary>
        /// Combined service - Both PDF and Excel allowed
        /// </summary>
        Combined = 3
    }
}
