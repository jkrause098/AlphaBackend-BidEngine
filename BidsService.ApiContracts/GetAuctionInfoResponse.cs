using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class GetAuctionInfoResponse : ApiResponseBase<AuctionStatus>
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that is being requested.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the internal number associated with the auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public int AuctionNo { get; set; }

    /// <summary>
    /// Gets or sets the date and time in the UTC time zone determining when the first item (lot) will be closed.
    /// </summary>
    [DataMember]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? FirstItemScheduledCloseUtc { get; set; }

    /// <summary>
    /// Gets or sets the collection of bid increments associated with the auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public BidIncrement[] BidIncrements { get; set; }

    /// <summary>
    /// Gets or sets the collection of lots (items) associated with the auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public LotDetails[] Lots { get; set; }
}
