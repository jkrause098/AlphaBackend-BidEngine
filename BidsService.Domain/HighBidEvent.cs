using System.Runtime.Serialization;
using Newtonsoft.Json;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Defines an event that gets triggered when a high bid is placed on an item.
/// </summary>
[DataContract]
public class HighBidEvent
{
    /// <summary>
    /// Gets or sets the ID of the auction.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public LotStatus ItemStatus { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who placed this bid.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the bidding user.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the location (US state) of the bidding user.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string UserState { get; set; }

    /// <summary>
    /// Gets or sets the paddle number associated with the bidding user.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string UserPaddleNo { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public decimal BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this bid was placed.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public DateTimeOffset BidDateTime { get; set; }

    /// <summary>
    /// Gets or sets the next acceptable bid amount.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public decimal NextBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [DataMember]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? ScheduledCloseDate { get; set; }
}
