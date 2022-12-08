using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Domain;

namespace AuctionPlatform.BidsService.LotActor;

internal static class BusinessRules
{
    public static bool CanExtendScheduledCloseDate(this AuctionLot lot)
    {
        return lot.IsTimedAuction() &&
               lot.ScheduledCloseDate.HasValue &&
               lot.ClosingInterval.HasValue &&
               lot.ScheduledCloseDate.Value >= DateTimeOffset.UtcNow &&
               lot.ScheduledCloseDate.Value.Subtract(DateTimeOffset.UtcNow) <= lot.ClosingInterval.Value;
    }

    public static BidIncrement GetBidIncrement(this AuctionLot lot, decimal? bidAmount)
    {
        // If no increment is defined, assume a default one.
        return (!lot.BidIncrement.HasValue && bidAmount.HasValue
               ? lot.BidIncrements.Where(i => bidAmount >= i.Low && bidAmount <= (i.High ?? decimal.MaxValue)).FirstOrDefault() : default)
               ?? new BidIncrement { Increment = lot.BidIncrement ?? 1 };
    }

    public static decimal GetNextBidAmount(this AuctionLot lot, decimal currentPrice)
    {
        var incremenent = lot.GetBidIncrement(currentPrice).Increment;
        return currentPrice + incremenent - (currentPrice % incremenent);
    }

    public static bool IsTimedAuction(this AuctionLot lot)
    {
        return lot != null && lot.AuctionType != null && string.Compare(lot.AuctionType, "Timed", StringComparison.InvariantCultureIgnoreCase) == 0;
    }
}
