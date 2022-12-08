using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Defines an event that will be fired by Bidding Engine and sent to SignalR when
/// a new choice lot group has been created and is ready to accept bids.
/// </summary>
public class ChoiceLotCreatedEvent
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
    /// Gets or sets the ID of the choice lot group.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets the number of lots that have been linked to this choice lot group.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public int LotCount { get; set; }

    /// <summary>
    /// Gets or sets the collection of lots (items) associated with the choice lot group.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public LotDetails[] Lots { get; set; }
}
