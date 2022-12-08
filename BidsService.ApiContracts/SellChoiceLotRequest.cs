using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class SellChoiceLotRequest
{
    /// <summary>
    /// Gets or sets the ID of the choice lot group which is being marked as sold.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the auction's lots being sold as part of choice lot group.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string[] ItemIds { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who won the choice lot.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string UserId { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }

    /// <summary>
    /// Gets or sets the unique ID of the auction that owns the choice lot.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string AuctionId { get; set; }
}
