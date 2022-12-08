using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenRingRequest
{
    /// <summary>
    /// Gets or sets the unique ID of the ring that is being opened.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string RingId { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
