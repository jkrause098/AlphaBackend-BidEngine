using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;
using SullivanAuctioneers.Common.Data;

namespace AuctionPlatform.BidsService.Domain.Transforms;

#pragma warning disable CS8603 // Possible null reference return.
public class BidLogEntryTransform : ITransformer<PlaceBidRequest, PlaceBidResponse, BidLogEntry>,
                                    ITransformer<HighBidInfo, BidLogEntry>
{
    public BidLogEntry Transform(PlaceBidRequest input1, PlaceBidResponse input2)
    {
        // Protect against bad input
        if (input1 == null) return default;

        return new BidLogEntry
        {
            BidId = input2?.BidId ?? Guid.NewGuid().ToUpperCase(),
            RequestId = input1.RequestId,
            AuctionId = input1.AuctionId,
            ItemId = input1.ItemId,
            UserId = input1.UserId,
            UserName = input1.UserName,
            UserState = input1.UserState,
            UserPaddleNo = input1.UserPaddleNo,
            BidAmount = input2?.BidAmount ?? input1.BidAmount,
            MaxBidAmount = input1.MaxBidAmount,
            LowBidAmount = input2?.LowBidAmount,
            HighBidAmount = input2?.HighBidAmount,
            NextBidAmount = input2?.NextBidAmount,
            ItemPrice = input2?.ItemPrice,
            BidStatus = input2?.Status ?? BidStatus.Invalid,
            BidDateTime = input2?.BidDateTime ?? DateTimeOffset.UtcNow
        };
    }

    public BidLogEntry Transform(HighBidInfo input)
    {
        // Protect against bad input
        if (input == null) return default;

        return new BidLogEntry
        {
            BidId = input.BidId,
            RequestId = Guid.NewGuid().ToUpperCase(),
            AuctionId = input.AuctionId,
            ItemId = input.ItemId,
            UserId = input.UserId,
            UserName = input.UserName,
            UserState = input.UserState,
            UserPaddleNo = input.UserPaddleNo,
            BidAmount = input.BidAmount,
            MaxBidAmount = input.MaxBidAmount,
            NextBidAmount = input.NextBidAmount,
            ItemPrice = input.BidAmount,
            BidStatus = BidStatus.Accepted,
            BidDateTime = input.BidDateTime
        };
    }
}
#pragma warning restore CS8603 // Possible null reference return.
