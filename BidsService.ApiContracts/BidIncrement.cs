using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class BidIncrement
{
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public decimal Low { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? High { get; set; }

    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public int Increment { get; set; }
}
