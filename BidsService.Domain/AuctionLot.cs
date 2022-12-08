using System.Runtime.Serialization;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Implements an object holding information about an auction lot.
/// </summary>
[DataContract]
public class AuctionLot
{
    /// <summary>
    /// Gets or sets the unique ID of the auction that this lot belongs to.
    /// </summary>
    [DataMember]
    public Guid AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the type of auction (such as timed, virtual, etc.)
    /// </summary>
    [DataMember]
    public string AuctionType { get; set; }

    /// <summary>
    /// Gets or sets the item ID which this lot represents.
    /// </summary>
    [DataMember]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the lot number.
    /// </summary>
    [DataMember]
    public string ItemLotNo { get; set; }

    /// <summary>
    /// Gets or sets the lot's seller ID.
    /// </summary>
    [DataMember]
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the lot will be closed, or expire.
    /// </summary>
    [DataMember]
    public DateTimeOffset? ScheduledCloseDate { get; set; }

    /// <summary>
    /// Gets or sets the amount of time during which the lot will be considered as being in the "closing" phase.
    /// </summary>
    [DataMember]
    public TimeSpan? ClosingInterval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the amount of time that will be used for extending the lot's closing phase.
    /// </summary>
    [DataMember]
    public TimeSpan? ClosingExtension { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the initial asking price for this lot.
    /// </summary>
    [DataMember]
    public int? InitialAskingPrice { get; set; }

    /// <summary>
    /// Gets or sets the initial high bid amount for this lot.
    /// </summary>
    [DataMember]
    public int? InitialHighBid { get; set; }

    /// <summary>
    /// Gets or sets the minimum bid for this lot.
    /// </summary>
    [DataMember]
    public int? MinimumBid { get; set; }

    /// <summary>
    /// Gets or sets the custom bid increment of this lot.
    /// </summary>
    [DataMember]
    public int? BidIncrement { get; set; }

    /// <summary>
    /// Gets or sets the collection of bid increments configured for this lot.
    /// </summary>
    [DataMember]
    public BidIncrement[] BidIncrements { get; set; }

    /// <summary>
    /// Gets or sets the current status of this lot.
    /// </summary>
    [DataMember]
    public LotStatus Status { get; set; }
}
