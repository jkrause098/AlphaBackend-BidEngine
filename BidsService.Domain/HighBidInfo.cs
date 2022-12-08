using System.Runtime.Serialization;
using Newtonsoft.Json;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Defines an internal state object that created when a high bid is placed on an item.
/// </summary>
[DataContract]
public class HighBidInfo
{
    /// <summary>
    /// Gets or sets the unique ID of the bid.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public string BidId { get; set; }

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
    /// Gets or sets the maximum bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? MaxBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [DataMember]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? ScheduledCloseDate { get; set; }

    /// <summary>
    /// Gets or sets the custom bid increment of this lot (if set).
    /// </summary>
    [DataMember]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? BidIncrement { get; set; }
}
