namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Provides a set of reusable constants that need to be accessible by all services.
/// </summary>
public static class ApiConstants
{
    /// <summary>
    /// Consolidates all well-known JSON property names in a single convenient location.
    /// </summary>
    public struct JsonPropertyNames
    {
        /// <summary>
        /// Provides a JSON property name for the Status property.
        /// </summary>
        public const string Status = "status";

        /// <summary>
        /// Provides a JSON property name for the Message property.
        /// </summary>
        public const string Message = "msg";

        /// <summary>
        /// Provides a JSON property name for the Request ID property.
        /// </summary>
        public const string RequestId = "reqID";

        /// <summary>
        /// Provides a JSON property name for the Result property.
        /// </summary>
        public const string Result = "result";

        /// <summary>
        /// Provides a JSON property name for the Success property.
        /// </summary>
        public const string Success = "success";

        /// <summary>
        /// Provides a JSON property name for the Errors property.
        /// </summary>
        public const string Errors = "err";

        /// <summary>
        /// Provides a JSON property name for the MessageCode property.
        /// </summary>
        public const string MessageCode = "msgCode";
    }
}
