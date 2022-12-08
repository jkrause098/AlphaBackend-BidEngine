using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenAuctionResponse : ApiResponseBase<AuctionStatus>
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that is being requested.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }
}
