using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;
using SullivanAuctioneers.Common.Data;

namespace AuctionPlatform.BidsService.Domain.Transforms;

#pragma warning disable CS8603 // Possible null reference return.
public class BidInfoTransform : ITransformer<LotUpdatedEvent[], BidInfo>
{
    public BidInfo Transform(LotUpdatedEvent[] updates)
    {
        // Protect against bad input
        if (updates == null || updates.Length == 0) return default;

        var sortedList = updates.OrderByDescending(i => i.EventDateTime).ToArray();
        var lastStatus = sortedList.FirstOrDefault(i => i.ItemStatus.HasValue)?.ItemStatus;
        var lastNextBidAmount = sortedList.FirstOrDefault(i => i.NextBidAmount.HasValue)?.NextBidAmount;
        var lastScheduledCloseDate = sortedList.FirstOrDefault(i => i.ScheduledCloseDate.HasValue)?.ScheduledCloseDate;
        var lastBidIncrement = sortedList.FirstOrDefault(i => i.BidIncrement.HasValue)?.BidIncrement;

        return new BidInfo
        {
            ItemId = updates.First().ItemId.ToUpperCase(),
            ItemStatus = lastStatus.HasValue ? (int)lastStatus : (int)LotStatus.NoBid,
            UserId = string.Empty,
            UserName = string.Empty,
            UserState = string.Empty,
            UserPaddleNo = string.Empty,
            BidAmount = 0,
            BidDateTime = DateTimeOffset.MinValue,
            NextBidAmount = lastNextBidAmount ?? 0,
            ScheduledCloseDate = lastScheduledCloseDate,
            BidIncrement = lastBidIncrement
        };
    }
}
#pragma warning restore CS8603 // Possible null reference return.
