using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Defines an event that will be fired by Bidding Engine and sent to SignalR when
/// bidding is closed for a given auction and all of its lots.
/// </summary>
public class AuctionClosedEvent
{
    /// <summary>
    /// Gets or sets the date and time when the event was created.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public DateTimeOffset EventDateTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the unique ID of the auction for which this event is triggered.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public Guid AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the internal number associated with the auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public int AuctionNo { get; set; }

    /// <summary>
    /// Gets or sets the number of lots that have been linked to this auction at the time of closure.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public int LotCount { get; set; }
}
