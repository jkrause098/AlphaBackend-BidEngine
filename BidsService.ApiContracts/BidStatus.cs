namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Represents one of the potential statuses for a bid.
/// </summary>
public enum BidStatus
{
    /// <summary>
    /// Represents a Invalid status.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// Represents a Accepted status.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Represents a AcceptedOutbid status.
    /// </summary>
    /// <remarks>
    /// This status can be returned when a placed bid gets automatically
    /// overridden by a higher bid, usually placed earlier as a max bid.
    /// </remarks>
    AcceptedOutbid = 2,

    /// <summary>
    /// Represents a Rejected status.
    /// </summary>
    Rejected = -1,

    /// <summary>
    /// Represents a NotFound status.
    /// </summary>
    NotFound = -2
}
