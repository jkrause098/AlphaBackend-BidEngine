using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class LotDetails
{
    /// <summary>
    /// Gets or sets the item ID which this lot represents.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the lot number.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public string ItemLotNo { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public DateTimeOffset? ScheduledCloseDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    [DataMember]
    public LotStatus Status { get; set; }
}
