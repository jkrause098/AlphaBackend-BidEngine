using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class UpdateLotRequest
{
    /// <summary>
    /// Gets or sets the ID of the auction's lot being updated.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [JsonProperty]
    [DataMember]
    public DateTimeOffset? ScheduledCloseDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    /// <remarks>
    /// This value is mapped to the ItemBidStatusId field in the database.
    /// </remarks>
    [JsonProperty]
    [DataMember]
    public LotStatus? LotStatus { get; set; }

    /// <summary>
    /// Gets or sets the custom bid increment of this lot.
    /// </summary>
    /// <remarks>
    /// If specified, this value will override any bid increments configured for the auction.
    /// </remarks>
    [JsonProperty]
    [DataMember]
    public int? BidIncrement { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
