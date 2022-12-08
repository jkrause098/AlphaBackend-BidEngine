using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class CloseAuctionResponse : ApiResponseBase<AuctionStatus>
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that was closed.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }
}
