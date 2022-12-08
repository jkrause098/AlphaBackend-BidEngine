using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class UpdateRingResponse : ApiResponse
{
    /// <summary>
    /// Gets or sets the unique ID of the auction which updated ring is currently part of.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the auction ring that was updated.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string RingId { get; set; }
}
