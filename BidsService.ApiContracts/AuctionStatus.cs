namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Represents one of the potential statuses for an auction.
/// </summary>
public enum AuctionStatus
{
    /// <summary>
    /// Represents a NotOpened status.
    /// </summary>
    NotOpened = 0,

    /// <summary>
    /// Represents a Opened status.
    /// </summary>
    Opened = 1,

    /// <summary>
    /// Represents a Closed status.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// Represents a AlreadyOpened status.
    /// </summary>
    AlreadyOpened = 3,

    /// <summary>
    /// Represents a AlreadyClosed status.
    /// </summary>
    AlreadyClosed = 4,

    /// <summary>
    /// Represents a NotFound status.
    /// </summary>
    NotFound = -1
}
