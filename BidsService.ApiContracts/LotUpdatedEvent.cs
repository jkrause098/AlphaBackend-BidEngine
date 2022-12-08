using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Defines an event that will be fired by Bidding Engine and sent to SignalR when
/// some information is updated on a given auction lot.
/// </summary>
[DataContract]
public class LotUpdatedEvent
{
    /// <summary>
    /// Gets or sets the date and time when the event was created.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public DateTimeOffset EventDateTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the unique ID of the auction that this lot belongs to.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public Guid AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    /// <remarks>
    /// This value is mapped to the ItemBidStatusId field in the database.
    /// </remarks>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public LotStatus? ItemStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot was closed, or expired.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public DateTimeOffset? ScheduledCloseDate { get; set; }

    /// <summary>
    /// Gets or sets the custom bid increment of this lot.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public int? BidIncrement { get; set; }

    /// <summary>
    /// Gets or sets the next acceptable bid amount. This value will be included in the event's payload
    /// whenever a new custom bid increment is updated.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? NextBidAmount { get; set; }
}
