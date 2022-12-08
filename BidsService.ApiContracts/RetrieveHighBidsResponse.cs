using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class RetrieveHighBidsResponse : ApiResponseBase<ApiResponseStatus>
{
    public RetrieveHighBidsResponse() => Bids = Array.Empty<BidInfo>();

    /// <summary>
    /// Gets or sets the unique ID of the auction holding the items in question.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the collection of current high bids for this lot.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public BidInfo[] Bids { get; set; }
}
