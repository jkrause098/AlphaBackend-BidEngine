namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Represents one of the potential statuses for generic API responses.
/// </summary>
public enum ApiResponseStatus
{
    /// <summary>
    /// Represents a success status.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Represents a failure status.
    /// </summary>
    Failure = -1,

    /// <summary>
    /// Represents a status indicating that requested object was not found.
    /// </summary>
    NotFound = -2
}
