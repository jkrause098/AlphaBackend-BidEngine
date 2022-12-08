using SullivanAuctioneers.Common.Data;

namespace AuctionPlatform.BidsService.Domain.Transforms;

#pragma warning disable CS8603 // Possible null reference return.
public class HighBidEventTransform : ITransformer<HighBidInfo, HighBidEvent>
{
    public HighBidEvent Transform(HighBidInfo highBidInfo)
    {
        // Protect against bad input
        if (highBidInfo == null) return default;

        return new HighBidEvent
        {
            AuctionId = highBidInfo.AuctionId,
            ItemId = highBidInfo.ItemId,
            ItemStatus = highBidInfo.ItemStatus,
            UserId = highBidInfo.UserId,
            UserName = highBidInfo.UserName,
            UserState = highBidInfo.UserState,
            UserPaddleNo = highBidInfo.UserPaddleNo,
            BidAmount = highBidInfo.BidAmount,
            BidDateTime = highBidInfo.BidDateTime,
            NextBidAmount = highBidInfo.NextBidAmount,
            ScheduledCloseDate = highBidInfo.ScheduledCloseDate
        };
    }
}
#pragma warning restore CS8603 // Possible null reference return.
