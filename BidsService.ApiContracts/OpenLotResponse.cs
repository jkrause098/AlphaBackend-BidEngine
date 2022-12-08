using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenLotResponse : ApiResponse
{
    /// <summary>
    /// Gets or sets the unique ID of the auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the unique ID of the auction lot (item).
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string ItemId { get; set; }
}
