using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class SellLotRequest
{
    /// <summary>
    /// Gets or sets the ID of the auction's lot being sold.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
