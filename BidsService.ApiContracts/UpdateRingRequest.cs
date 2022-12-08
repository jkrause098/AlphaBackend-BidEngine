using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class UpdateRingRequest
{
    /// <summary>
    /// Gets or sets the ID of the auction ring being updated.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RingId { get; set; }

    /// <summary>
    /// Gets or sets the flag indicating that caller is requesting to refresh all lots that belong to this ring
    /// by reloading their date and time when each lot will be closed (or expire).
    /// </summary>
    [JsonProperty]
    [DataMember]
    public bool? ReloadScheduledCloseDate { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonIgnore]
    [DataMember]
    public string RequestId { get; set; }
}
