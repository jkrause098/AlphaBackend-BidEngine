using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenRingResponse : ApiResponseBase<AuctionStatus>
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that was processed by original request.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the unique ID of the ring that was processed by original request.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string RingId { get; set; }
}
