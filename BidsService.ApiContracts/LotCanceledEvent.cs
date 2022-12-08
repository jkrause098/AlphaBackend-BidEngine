using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Defines an event that will be fired by Bidding Engine and sent to SignalR when
/// a given auction lot is canceled and removed from the open auction.
/// </summary>
public class LotCanceledEvent
{
    /// <summary>
    /// Gets or sets the date and time when the event was created.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public DateTimeOffset EventDateTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the unique ID of the auction that this lot belongs to.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public Guid AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who placed the high bid (if any).
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the high bid amount (if any).
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the high bid was placed (if any).
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? BidDateTime { get; set; }
}
