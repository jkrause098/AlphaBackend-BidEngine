using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class PlaceBidResponse : ApiResponseBase<BidStatus>
{
    /// <summary>
    /// Gets or sets the unique ID of the bid.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string BidId { get; set; }

    /// <summary>
    /// Gets or sets the unique ID of the auction that is being requested.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the auction's lot being bid.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this bid was placed.
    /// </summary>
    [DataMember]
    [JsonProperty(Required = Required.Always)]
    public DateTimeOffset BidDateTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the lowest acceptable bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? LowBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the highest acceptable bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? HighBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the next acceptable bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? NextBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the item price after bid placement.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public decimal ItemPrice { get; set; }
}
