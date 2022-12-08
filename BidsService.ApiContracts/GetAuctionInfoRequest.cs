using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class GetAuctionInfoRequest
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that is being opened.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
