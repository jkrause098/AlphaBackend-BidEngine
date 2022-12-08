using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenChoiceLotResponse : ApiResponse
{
    /// <summary>
    /// Gets or sets the unique ID of the auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the choice lot group which must exist in the database.
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    [DataMember]
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets the collection of lots (items) associated with the auction.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public LotDetails[] Lots { get; set; }
}
