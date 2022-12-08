using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class DeleteBidsResponse : ApiResponseBase<ApiResponseStatus>
{
    public DeleteBidsResponse() => Bids = Array.Empty<BidInfo>();

    /// <summary>
    /// Gets or sets the ID of the auction's lot that was bid.
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item's current price.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets or sets the collection of removed bids.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public BidInfo[] Bids { get; set; }
}
