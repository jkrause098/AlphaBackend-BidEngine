using AuctionPlatform.BidsService.ApiContracts;
using SullivanAuctioneers.Common.Data;
using DM = AuctionPlatform.Domain.Entities;

namespace AuctionPlatform.BidsService.Domain.Transforms;

#pragma warning disable CS8603 // Possible null reference return.
public class BidIncrementTransform : ITransformer<DM.BidIncrementTableDetail, BidIncrement>
{
    public BidIncrement Transform(DM.BidIncrementTableDetail input)
    {
        // Protect against bad input
        if (input == null) return default;

        return new BidIncrement
        {
            Low = input.Low,
            High = input.High,
            Increment = input.Increment
        };
    }
}
#pragma warning restore CS8603 // Possible null reference return.
