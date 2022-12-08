using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class RetrieveHighBidsRequest
{
    /// <summary>
    /// Gets or sets the unique ID of the auction holding the items in question.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the array of auction lot IDs for which high bids are being requested.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string[] ItemIds { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
