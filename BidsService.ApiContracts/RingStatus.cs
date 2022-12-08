namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Represents one of the potential statuses for a ring.
/// </summary>
public enum RingStatus
{
    /// <summary>
    /// Represents a Closed status.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Represents a Live status.
    /// </summary>
    Live = 1,

    /// <summary>
    /// Represents a Finished status.
    /// </summary>
    Finished = 3
}
