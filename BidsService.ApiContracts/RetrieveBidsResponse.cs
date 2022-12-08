using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class RetrieveBidsResponse : ApiResponseBase<ApiResponseStatus>
{
    public RetrieveBidsResponse() => Bids = Array.Empty<BidInfo>();

    /// <summary>
    /// Gets or sets the ID of the auction's lot that was bid.
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item's initial price.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public decimal StartingPrice { get; set; }

    /// <summary>
    /// Gets or sets the item's current price.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets or sets the custom bid increment of this lot (if set).
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public int? BidIncrement { get; set; }

    /// <summary>
    /// Gets or sets the collection of current bids for this lot.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public BidInfo[] Bids { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public DateTimeOffset? ScheduledCloseDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public int? ItemStatus { get; set; }
}

[DataContract]
public class BidInfo
{
    /// <summary>
    /// Gets or sets the ID of the auction's lot that this bid is placed for.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 0)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    /// <remarks>
    /// This value is mapped to the ItemBidStatusId entity in the database.
    /// </remarks>
    [DataMember]
    [JsonProperty(Required = Required.Always, Order = 1)]
    public int ItemStatus { get; set; }

    /// <summary>
    /// Gets or sets the ID of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 2)]
    [DataMember]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 3)]
    [DataMember]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the location (US state) of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 4)]
    [DataMember]
    public string UserState { get; set; }

    /// <summary>
    /// Gets or sets the paddle number associated with the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 5)]
    [DataMember]
    public string UserPaddleNo { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 6)]
    [DataMember]
    public decimal BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the next acceptable bid amount.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always, Order = 7)]
    public decimal NextBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 8)]
    [DataMember]
    public decimal? MaxBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this bid was placed.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 9)]
    [DataMember]
    public DateTimeOffset BidDateTime { get; set; }

    /// <summary>
    /// Gets the sequence number associated with the bid (can be used for quick and reliable sorting of bids)
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 10)]
    [DataMember]
    public long BidSequenceNo { get { return BidDateTime.Ticks; } private set { } }

    /// <summary>
    /// Gets or sets the status of this bid.
    /// </summary>
    [JsonProperty(Required = Required.Always, Order = 11)]
    [DataMember]
    public BidStatus BidStatus { get; set; }

    /// <summary>
    /// Gets or sets the custom bid increment of this lot (if set).
    /// </summary>
    [DataMember]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 12)]
    public int? BidIncrement { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [DataMember]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 13)]
    public DateTimeOffset? ScheduledCloseDate { get; set; }
}
