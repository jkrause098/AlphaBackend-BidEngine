using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class SellChoiceLotResponse : SellLotResponse
{
    /// <summary>
    /// Gets or sets the ID of the choice lot group which is being marked as sold.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string GroupId { get; set; }
}
