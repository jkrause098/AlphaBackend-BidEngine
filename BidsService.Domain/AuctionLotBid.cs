using System.Runtime.Serialization;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Implements an object holding information about a single bid on an auction lot.
/// </summary>
[DataContract]
public class AuctionLotBid
{
    /// <summary>
    /// Gets or sets the ID of the bid.
    /// </summary>
    [DataMember]
    public string BidId { get; private set; } = Guid.NewGuid().ToUpperCase();

    /// <summary>
    /// Gets or sets the ID of the user who placed this bid.
    /// </summary>
    [DataMember]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the bidding user.
    /// </summary>
    [DataMember]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the location (US state) of the bidding user.
    /// </summary>
    [DataMember]
    public string UserState { get; set; }

    /// <summary>
    /// Gets or sets the paddle number associated with the bidding user.
    /// </summary>
    [DataMember]
    public string UserPaddleNo { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    [DataMember]
    public decimal BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum bid amount.
    /// </summary>
    [DataMember]
    public decimal? MaxBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this bid was placed.
    /// </summary>
    [DataMember]
    public DateTimeOffset BidDateTime { get; set; }

    /// <summary>
    /// Gets or sets the status of this bid.
    /// </summary>
    [DataMember]
    public BidStatus Status { get; set; }
}
