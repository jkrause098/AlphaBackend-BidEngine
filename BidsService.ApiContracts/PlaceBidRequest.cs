using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class PlaceBidRequest
{
    /// <summary>
    /// Gets or sets the unique ID of the open auction.
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
    /// Gets or sets the ID of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the location (US state) of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string UserState { get; set; }

    /// <summary>
    /// Gets or sets the paddle number associated with the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string UserPaddleNo { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    /// <remarks>
    /// The caller must specify either <see cref="BidAmount"/> or <see cref="MaxBidAmount"/>.
    /// </remarks>
    [JsonProperty(Required = Required.DisallowNull)]
    [DataMember]
    public decimal? BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the max bid amount.
    /// </summary>
    /// <remarks>
    /// The caller must specify either <see cref="BidAmount"/> or <see cref="MaxBidAmount"/>.
    /// </remarks>
    [JsonProperty(Required = Required.DisallowNull)]
    [DataMember]
    public decimal? MaxBidAmount { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
