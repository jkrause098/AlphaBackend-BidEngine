using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class DeleteBidsRequest
{
    /// <summary>
    /// Gets or sets the ID of the auction's lot for which bids are being requested.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or set a boolean flag indicating that only last bid must be removed (if present).
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public bool LastOnly { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
