using System.Runtime.Serialization;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Defines an informational object describing an auction.
/// </summary>
[DataContract]
public class AuctionInfo
{
    /// <summary>
    /// Gets or sets the ID of the auction.
    /// </summary>
    [DataMember]
    public Guid AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the auction's ring.
    /// </summary>
    [DataMember]
    public Guid RingId { get; set; }

    /// <summary>
    /// Gets or sets the internal number associated with the auction.
    /// </summary>
    [DataMember]
    public int AuctionNo { get; set; }

    /// <summary>
    /// Gets or sets the type of auction (such as timed, virtual, etc.)
    /// </summary>
    [DataMember]
    public string AuctionType { get; set; }

    /// <summary>
    /// Gets or sets the date and time in the UTC time zone determining when the first item (lot) will be closed.
    /// </summary>
    [DataMember]
    public DateTime? FirstItemScheduledCloseUtc { get; set; }

    /// <summary>
    /// Gets or sets the collection of bid increments associated with the auction.
    /// </summary>
    [DataMember]
    public BidIncrement[] BidIncrements { get; set; }

    /// <summary>
    /// Gets or sets the collection of lots (items) associated with the auction.
    /// </summary>
    [DataMember]
    public AuctionLot[] Lots { get; set; }
}
