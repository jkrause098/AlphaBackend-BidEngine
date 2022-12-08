using AuctionPlatform.BidsService.Domain;
using DM = AuctionPlatform.Domain.Entities;

namespace AuctionPlatform.BidsService.AuctionController;

internal static class BusinessRules
{
    public static IEnumerable<DM.ItemBid> CreateItemBids(AuctionLot lot, List<AuctionLotBid> bids)
    {
        if (bids == null || bids.Count == 0) return Enumerable.Empty<DM.ItemBid>();

        return bids.Select(bid => new DM.ItemBid
        {
            Id = Guid.Parse(bid.BidId),
            AuctionId = lot.AuctionId,
            ItemId = lot.ItemId,
            AuctionBidderId = Guid.Parse(bid.UserId),
            BidDateTime = bid.BidDateTime.UtcDateTime,
            BidAmount = Convert.ToInt32(bid.BidAmount),
            MaxBidAmount = bid.MaxBidAmount.HasValue ? Convert.ToInt32(bid.MaxBidAmount.Value) : null,
            UserName = bid.UserName,
            UserLocation = bid.UserState,
            UserPaddleNo = bid.UserPaddleNo,
            BidStatusId = (int)bid.Status,
            CreatedDate = DateTime.UtcNow
        });
    }
}
