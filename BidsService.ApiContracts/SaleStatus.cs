namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Represents one of the potential statuses for a sale.
/// </summary>
public enum SaleStatus
{
    /// <summary>
    /// Represents a Invalid status.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// Represents a Sold status.
    /// </summary>
    Sold = 1,

    /// <summary>
    /// Represents a Rejected status.
    /// </summary>
    Rejected = -1,

    /// <summary>
    /// Represents a NotFound status.
    /// </summary>
    NotFound = -2,

    /// <summary>
    /// Represents a Canceled status.
    /// </summary>
    Canceled = -3,
}
