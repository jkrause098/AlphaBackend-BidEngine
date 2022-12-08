using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class SellLotResponse : ApiResponseBase<SaleStatus>
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that is being requested.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the auction's lot that was sold.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who purchased the lot.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the sale price.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public decimal? ItemPrice { get; set; }
}
