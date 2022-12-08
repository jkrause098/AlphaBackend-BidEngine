using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenChoiceLotRequest
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that owns the choice lot.
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the choice lot group which must exist in the database.
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    [DataMember]
    public string GroupId { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
