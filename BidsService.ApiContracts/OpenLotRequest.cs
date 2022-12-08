using System.Runtime.Serialization;

namespace AuctionPlatform.BidsService.ApiContracts;

[DataContract]
public class OpenLotRequest
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that is being opened.
    /// </summary>
    [DataMember]
    public string AuctionId { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [DataMember]
    public string RequestId { get; set; }
}
