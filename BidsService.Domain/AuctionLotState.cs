using System.Runtime.Serialization;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Implements an object holding information about all bids made to a particular auction lot.
/// </summary>
[DataContract]
public class AuctionLotState
{
    [DataMember]
    public List<AuctionLotBid> Bids { get; set; }

    /// <summary>
    /// Gets or sets the item's initial price.
    /// </summary>
    [DataMember]
    public decimal StartingPrice { get; set; }

    /// <summary>
    /// Gets or sets the item's current price.
    /// </summary>
    [IgnoreDataMember]
    public decimal CurrentPrice
    {
        get { return StartingPrice + HighBidAmount; }
        private set { }
    }

    [IgnoreDataMember]
    public decimal HighBidAmount
    {
        get
        {
            return Bids.Count > 0 ? Bids.Max(i => i.BidAmount) : decimal.Zero;
        }
        private set { }
    }

    [IgnoreDataMember]
    public AuctionLotBid? HighBid
    {
        get
        {
            return Bids.Count > 0 ? Bids.Last() : default;
        }
        private set { }
    }
}
