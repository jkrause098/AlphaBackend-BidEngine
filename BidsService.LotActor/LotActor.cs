using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using Serilog.Context;
using SerilogTimings.Extensions;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Common.Serialization;
using AuctionPlatform.BidsService.Domain;
using AuctionPlatform.BidsService.Infrastructure;
using AuctionPlatform.BidsService.Interfaces;

namespace AuctionPlatform.BidsService.LotActor;

/// <remarks>
/// Implements a resilient state-based Lot actor.
/// </remarks>
[StatePersistence(StatePersistence.Persisted)]
internal class LotActor : Actor, ILotActor, IRemindable
{
    private readonly IConfiguration configuration;
    private readonly ILogger logger;
    private readonly ISerializer serializer;
    private readonly CancellationTokenSource cts = new();
    private IAuctionController? auctionController;

    /// <summary>
    /// Initializes a new instance of LotActor
    /// </summary>
    /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
    /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
    public LotActor(ActorService actorService, ActorId actorId, IConfiguration configuration, ILogger logger, ISerializer serializer)
        : base(actorService, actorId)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.serializer = serializer;
    }

    /// <summary>
    /// This method is called whenever an actor is activated.
    /// An actor is activated the first time any of its methods are invoked.
    /// </summary>
    protected override Task OnActivateAsync()
    {
        logger.Debug("Lot actor {ItemId} has been activated", Id);

        var lotState = new AuctionLotState
        {
            Bids = new List<AuctionLotBid>()
        };

        return StateManager.TryAddStateAsync(nameof(AuctionLotState), lotState);
    }

    /// <summary>
    /// Override this method to release any resources. This method is called when actor
    /// is deactivated (garbage collected by Actor Runtime). Actor operations like state
    /// changes should not be called from this method.
    /// </summary>
    protected override Task OnDeactivateAsync()
    {
        logger.Debug("Lot actor {ItemId} has been deactivated", Id);

        cts.Cancel();
        base.OnDeactivateAsync();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<OpenLotResponse> OpenLotAsync(OpenLotRequest request, AuctionLot lot)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenLotAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, lot.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionType, lot.AuctionType))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, lot.ItemId))
        {
            try
            {
                // Make this method idempotent in case it is retried.
                if (await StateManager.TryAddStateAsync(nameof(AuctionLot), lot, cts.Token))
                {
                    // Register a reminder that will notify this item when it is about to be closed (for timed auctions only).
                    if (lot.IsTimedAuction() && lot.ScheduledCloseDate.HasValue && lot.ScheduledCloseDate.Value > DateTimeOffset.UtcNow)
                    {
                        await RegisterReminderAsync(GlobalConsts.ReminderNames.ScheduledClose, null, lot.ScheduledCloseDate.Value.Subtract(DateTimeOffset.UtcNow), TimeSpan.FromMilliseconds(-1));
                    }
                }

                return new OpenLotResponse
                {
                    Status = ApiResponseStatus.Success,
                    Success = true,
                    AuctionId = request.AuctionId,
                    ItemId = lot.ItemId.ToUpperCase(),
                    RequestId = request.RequestId,
                    Message = "The lot is ready to accept bids",
                    MessageCode = MessageCode.Backend.OpenLotAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while opening lot {ItemId} for auction {AuctionId}", lot.ItemId, request.AuctionId);

                return new OpenLotResponse
                {
                    Status = ApiResponseStatus.Failure,
                    Success = false,
                    AuctionId = request.AuctionId,
                    ItemId = lot.ItemId.ToUpperCase(),
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.OpenLotServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<PlaceBidResponse> PlaceBidAsync(PlaceBidRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(PlaceBidAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.UserId, request.UserId))
        {
            try
            {
                // Read the current state of the lot owned by this actor.
                var lotObject = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

                // Make sure this request is for the correct auction instance.
                if (!lotObject.HasValue || lotObject.Value.AuctionId != Guid.Parse(request.AuctionId))
                {
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Invalid,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        Message = "Bid cannot be accepted for an auction that is not currently opened",
                        MessageCode = MessageCode.Backend.PlaceBidLotNotOpened
                    };
                }

                // Extract the object stored in the lot's state for optimal performance.
                var lot = lotObject.Value;

                // Make sure this request is for the correct lot instance.
                if (lot.ItemId != Guid.Parse(request.ItemId))
                {
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Invalid,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        Message = "Bid cannot be accepted for an item that is not part of the specified auction",
                        MessageCode = MessageCode.Backend.PlaceBidInvalidLotState
                    };
                }

                // If the lot is paused for bidding, this request must be rejected.
                if (lot.Status == LotStatus.Paused)
                {
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Rejected,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        Message = "Bid cannot be accepted for an item that is paused for bidding",
                        MessageCode = MessageCode.Backend.PlaceBidLotPaused
                    };
                }

                // Use max bid amount as a bid amount in all subsequent computations.
                if (request.MaxBidAmount.HasValue)
                {
                    request.BidAmount = request.MaxBidAmount;
                }

                // Validate bid value to make sure it is not less than minimum accepted bid (if defined at lot level).
                if (lot.MinimumBid.HasValue && request.BidAmount < lot.MinimumBid.Value)
                {
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Rejected,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        Message = "The specified bid is lower than minimum bid value",
                        MessageCode = MessageCode.Backend.PlaceBidLowerThanMinimum
                    };
                }

                var lotStateObject = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                // Make sure this request is for the correct lot state.
                if (!lotStateObject.HasValue)
                {
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Invalid,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        Message = "Lot state is gone",
                        MessageCode = MessageCode.Backend.PlaceBidInvalidLotState
                    };
                }

                // Extract the object stored in the lot's state for optimal performance.
                var lotState = lotStateObject.Value;

                // Find the acceptable bid increment based on user's bid amount (only if a custom bid increment is not present).
                var bidIncrement = lot.GetBidIncrement(request.BidAmount);

                // Validate bid value to make sure it won't be accepted if it is lower than expected.
                if (lotState.HighBidAmount >= request.BidAmount)
                {
                    // This response should include the next bid amount to help the UI correctly render the next acceptable bid.
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Rejected,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        NextBidAmount = lotState.CurrentPrice + bidIncrement.Increment,
                        ItemPrice = lotState.CurrentPrice,
                        Message = $"The specified bid is lower than or equal to the current highest bid {lotState.HighBidAmount}",
                        MessageCode = MessageCode.Backend.PlaceBidLowerThanHighest
                    };
                }

                // Must only accept this bid if it complies with defined increment.
                if (request.BidAmount % bidIncrement.Increment != 0)
                {
                    return new PlaceBidResponse
                    {
                        Status = BidStatus.Rejected,
                        Success = false,
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidAmount = request.BidAmount,
                        LowBidAmount = request.BidAmount - (request.BidAmount % bidIncrement.Increment),
                        HighBidAmount = request.BidAmount - (request.BidAmount % bidIncrement.Increment) + bidIncrement.Increment,
                        ItemPrice = lotState.CurrentPrice,
                        Message = $"The specified bid is not aligned with increment ({bidIncrement.Increment}) allowed for this item",
                        MessageCode = MessageCode.Backend.PlaceBidWrongIncrement
                    };
                }

                // Compose a new bid that will be added to the list.
                var bid = new AuctionLotBid
                {
                    UserId = request.UserId,
                    UserName = request.UserName,
                    UserState = request.UserState,
                    UserPaddleNo = request.UserPaddleNo,
                    BidAmount = request.BidAmount ?? default,
                    MaxBidAmount = request.MaxBidAmount,
                    BidDateTime = DateTimeOffset.UtcNow,
                    Status = BidStatus.Accepted
                };

                // Determine if the user intends to place a max bid.
                if (request.MaxBidAmount.HasValue)
                {
                    // If lot has any current bids, we need to determine how to handle the max bid.
                    if (lotState.Bids.Count > 0)
                    {
                        // Grab the latest (highest) bid.
                        var highBid = lotState.Bids.Last();

                        // If the current user already owns the max bid, do not place a new bid, just update the max bid on the existing one.
                        if (highBid.UserId == bid.UserId)
                        {
                            bid.BidAmount = highBid.BidAmount;
                        }
                        else
                        {
                            // Find the max bid that is lower than the current max bid and is placed by some other user.
                            var lowerMaxBid = lotState.Bids.Where(b => b.MaxBidAmount.HasValue && b.UserId != request.UserId && b.MaxBidAmount < request.MaxBidAmount).OrderByDescending(b => b.MaxBidAmount).FirstOrDefault();

                            // If the current user is placing a max bid and it is higher than the current max bid from some other user,
                            // the current user's bid should be treated as a winning bid.
                            if (lowerMaxBid != null && lowerMaxBid.MaxBidAmount.HasValue && lowerMaxBid.MaxBidAmount > highBid.BidAmount)
                            {
                                // Work out the amount that will be used for the automatic bid.
                                bid.BidAmount = lowerMaxBid.MaxBidAmount.Value + lot.GetBidIncrement(lowerMaxBid.MaxBidAmount).Increment;
                            }
                            else
                            {
                                // Work out the amount that will be used for the automatic bid.
                                bid.BidAmount = highBid.BidAmount + lot.GetBidIncrement(highBid.BidAmount).Increment;
                            }
                        }
                    }
                    else
                    {
                        // By default, the bid amount will be adjusted to reflect the next incremental bid.
                        bid.BidAmount = lotState.CurrentPrice + lot.GetBidIncrement(lotState.CurrentPrice).Increment;
                    }
                }

                // Bid is now considered valid, let's add it to the list.
                lotState.Bids.Add(bid);

                // Find the max bid that exceeds the current bid and is placed by some other user.
                var maxBid = lotState.Bids.Where(b => b.MaxBidAmount.HasValue && b.UserId != request.UserId && b.MaxBidAmount >= bid.BidAmount).OrderByDescending(b => b.MaxBidAmount).FirstOrDefault();

                // If max bid is found, we need to inject a system-generated bid for the user who placed the max bid.
                if (maxBid != null && maxBid.MaxBidAmount.HasValue && request.BidAmount.HasValue)
                {
                    // Work out the amount that will be used for the automatic bid.
                    var autoBidAmount = Math.Min(request.BidAmount.Value + lot.GetBidIncrement(request.BidAmount).Increment, maxBid.MaxBidAmount.Value);

                    // Create the automatic bid.
                    var autoBid = new AuctionLotBid
                    {
                        UserId = maxBid.UserId,
                        UserName = maxBid.UserName,
                        UserState = maxBid.UserState,
                        UserPaddleNo = maxBid.UserPaddleNo,
                        BidAmount = autoBidAmount,
                        MaxBidAmount = maxBid.MaxBidAmount,
                        BidDateTime = bid.BidDateTime.AddMilliseconds(1),
                        Status = BidStatus.Accepted
                    };

                    // Only place the automatic bid if it is within the max bid value.
                    if (autoBid.BidAmount <= maxBid.MaxBidAmount)
                    {
                        // Create the automatic bid.
                        lotState.Bids.Add(autoBid);

                        // We should tell the original user that their bid has been accepted but it is not a winning bid anymore.
                        bid.Status = BidStatus.AcceptedOutbid;
                    }
                }

                // Save the updated bid collection to the Actor's state.
                await StateManager.AddOrUpdateStateAsync(nameof(AuctionLotState), lotState, (n, v) => lotState);

                // Now that the new bid is placed, it is time to figure out the next bid amount based on increment.
                var nextBidAmount = lotState.CurrentPrice + lot.GetBidIncrement(lotState.CurrentPrice).Increment;

                // Check if we are dealing with the bid that has been accepted as high bid, or outbid with a higher bid.
                if (bid.Status == BidStatus.Accepted || bid.Status == BidStatus.AcceptedOutbid)
                {
                    // Grab the latest (highest) bid and package it into a reminder object.
                    var highBid = lotState.Bids.Last();

                    // Compose a data object describing the high bid.
                    var highBidInfo = new HighBidInfo
                    {
                        AuctionId = request.AuctionId,
                        ItemId = request.ItemId,
                        ItemStatus = lot.Status,
                        UserId = highBid.UserId,
                        UserName = highBid.UserName,
                        UserState = highBid.UserState,
                        UserPaddleNo = highBid.UserPaddleNo,
                        BidId = highBid.BidId,
                        BidAmount = highBid.BidAmount,
                        MaxBidAmount = highBid.MaxBidAmount,
                        NextBidAmount = nextBidAmount,
                        BidDateTime = highBid.BidDateTime,
                        ScheduledCloseDate = lot.ScheduledCloseDate,
                        BidIncrement = lot.GetBidIncrement(lotState.CurrentPrice).Increment
                    };

                    // Create the reminder data object.
                    var reminderData = await serializer.Serialize(highBidInfo).ReadAllBytesAsync();

                    // Register the high bid with internal reminder queue for outbound delivery.
                    await RegisterReminderAsync(GlobalConsts.ReminderNames.HighBid, reminderData, TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(-1));
                }

                return new PlaceBidResponse
                {
                    Status = bid.Status,
                    Success = true,
                    BidId = bid.BidId,
                    BidDateTime = bid.BidDateTime,
                    AuctionId = request.AuctionId,
                    ItemId = request.ItemId,
                    ItemPrice = lotState.CurrentPrice,
                    RequestId = request.RequestId,
                    BidAmount = bid.BidAmount,
                    NextBidAmount = nextBidAmount,
                    Message = bid.Status == BidStatus.Accepted ? "Winning bid has been accepted" : "Bid has been accepted and immediately outbid",
                    MessageCode = bid.Status == BidStatus.Accepted ? MessageCode.Backend.PlaceBidAccepted : MessageCode.Backend.PlaceBidOutbid
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while placing a bid for {ItemId} in auction {AuctionId}", request.ItemId, request.AuctionId);

                return new PlaceBidResponse
                {
                    Status = BidStatus.Invalid,
                    Success = false,
                    AuctionId = request.AuctionId,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    BidAmount = request.BidAmount,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.PlaceBidServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<RetrieveBidsResponse> RetrieveBidsAsync(RetrieveBidsRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(RetrieveBidsAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        {
            try
            {
                // Read the current state of the lot owned by this actor.
                var lotObject = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

                // Make sure this request is for the correct lot instance.
                if (!lotObject.HasValue || lotObject.Value.ItemId != Guid.Parse(request.ItemId))
                {
                    return new RetrieveBidsResponse
                    {
                        Status = ApiResponseStatus.Failure,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "The specified item is not part any open auction",
                        MessageCode = MessageCode.Backend.RetrieveBidsInactiveLot
                    };
                }

                var lotStateObject = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                // Make sure this request is for the correct lot state.
                if (!lotStateObject.HasValue)
                {
                    return new RetrieveBidsResponse
                    {
                        Status = ApiResponseStatus.Failure,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "Lot state is gone",
                        MessageCode = MessageCode.Backend.RetrieveBidsInvalidLotState
                    };
                }

                // Extract the object stored in the lot's state for optimal performance.
                var lot = lotObject.Value;
                var lotState = lotStateObject.Value;

                // Make sure the item has any bid.
                if (lotState.Bids.Count == 0)
                {
                    return new RetrieveBidsResponse
                    {
                        Status = ApiResponseStatus.NotFound,
                        Success = true,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        BidIncrement = lot.BidIncrement,
                        ScheduledCloseDate = lot.ScheduledCloseDate,
                        ItemStatus = (int)lot.Status,
                        Message = "The specified item does not have any bids",
                        MessageCode = MessageCode.Backend.RetrieveBidsNoCurrentBids
                    };
                }

                return new RetrieveBidsResponse
                {
                    Status = ApiResponseStatus.Success,
                    Success = true,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    StartingPrice = lotState.StartingPrice,
                    CurrentPrice = lotState.CurrentPrice,
                    BidIncrement = lot.BidIncrement,
                    ScheduledCloseDate = lot.ScheduledCloseDate,
                    ItemStatus = (int)lot.Status,
                    MessageCode = MessageCode.Backend.RetrieveBidsOneOrMoreFound,
                    Bids = lotState.Bids.OrderBy(i => i.BidDateTime).Select(i => new BidInfo
                    {
                        UserId = i.UserId,
                        UserName = i.UserName,
                        UserState = i.UserState,
                        UserPaddleNo = i.UserPaddleNo,
                        BidAmount = i.BidAmount,
                        MaxBidAmount = i.MaxBidAmount,
                        BidDateTime = i.BidDateTime,
                        BidStatus = i.Status,
                        ItemStatus = (int)lot.Status,
                        ScheduledCloseDate = lot.ScheduledCloseDate,
                        BidIncrement = lot.BidIncrement
                    }).ToArray()
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while retrieving bids for {ItemId}", request.ItemId);

                return new RetrieveBidsResponse
                {
                    Status = ApiResponseStatus.Failure,
                    Success = false,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.RetrieveBidsServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<DeleteBidsResponse> DeleteBidsAsync(DeleteBidsRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(DeleteBidsAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        {
            try
            {
                // Read the current state of the lot owned by this actor.
                var lot = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

                // Make sure this request is for the correct lot instance.
                if (!lot.HasValue || lot.Value.ItemId != Guid.Parse(request.ItemId))
                {
                    return new DeleteBidsResponse
                    {
                        Status = ApiResponseStatus.Failure,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "The specified item is not part any open auction",
                        MessageCode = MessageCode.Backend.DeleteBidsInactiveLot
                    };
                }

                var lotState = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                // Make sure this request is for the correct lot state.
                if (!lotState.HasValue)
                {
                    return new DeleteBidsResponse
                    {
                        Status = ApiResponseStatus.Failure,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "Lot state is gone",
                        MessageCode = MessageCode.Backend.DeleteBidsInvalidLotState
                    };
                }

                // Make sure the item has any bid.
                if (lotState.Value.Bids.Count == 0)
                {
                    return new DeleteBidsResponse
                    {
                        Status = ApiResponseStatus.NotFound,
                        Success = true,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "The specified item does not have any bids",
                        MessageCode = MessageCode.Backend.DeleteBidsNoCurrentBids
                    };
                }

                // This is where we are going to track all deleted bids.
                var deletedBids = new List<BidInfo>();

                // Update bid list to exclude bids that are intended to be removed.
                if (request.LastOnly)
                {
                    var lastBid = lotState.Value.Bids.OrderBy(i => i.BidDateTime).Last();

                    deletedBids.Add(new BidInfo
                    {
                        UserId = lastBid.UserId,
                        UserName = lastBid.UserName,
                        UserState = lastBid.UserState,
                        UserPaddleNo = lastBid.UserPaddleNo,
                        BidAmount = lastBid.BidAmount,
                        MaxBidAmount = lastBid.MaxBidAmount,
                        BidDateTime = lastBid.BidDateTime
                    });

                    lotState.Value.Bids.RemoveAt(lotState.Value.Bids.Count - 1);
                }

                // Save the updated bid collection to the Actor's state.
                await StateManager.AddOrUpdateStateAsync(nameof(AuctionLotState), lotState.Value, (n, v) => lotState.Value);

                return new DeleteBidsResponse
                {
                    Status = ApiResponseStatus.Success,
                    Success = true,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    CurrentPrice = lotState.Value.CurrentPrice,
                    Bids = deletedBids.ToArray(),
                    MessageCode = MessageCode.Backend.DeleteBidsAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while deleting bids for {ItemId}", request.ItemId);

                return new DeleteBidsResponse
                {
                    Status = ApiResponseStatus.Failure,
                    Success = false,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.DeleteBidsServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<SellLotResponse> SellLotAsync(SellLotRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(SellLotAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        {
            try
            {
                // Read the current state of the lot owned by this actor.
                var lot = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

                // Make sure this request is for the correct lot instance.
                if (!lot.HasValue || lot.Value.ItemId != Guid.Parse(request.ItemId))
                {
                    return new SellLotResponse
                    {
                        Status = SaleStatus.NotFound,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "The specified item is not part any open auction",
                        MessageCode = MessageCode.Backend.SellLotInactiveLot
                    };
                }

                var lotState = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                // Make sure this request is for the correct lot state.
                if (!lotState.HasValue)
                {
                    return new SellLotResponse
                    {
                        Status = SaleStatus.NotFound,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        AuctionId = lot.Value.AuctionId.ToUpperCase(),
                        Message = "Lot state is gone",
                        MessageCode = MessageCode.Backend.SellLotInvalidLotState
                    };
                }

                // Make sure the item has any bid.
                if (lotState.Value.Bids.Count == 0)
                {
                    return new SellLotResponse
                    {
                        Status = SaleStatus.Rejected,
                        Success = true,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        AuctionId = lot.Value.AuctionId.ToUpperCase(),
                        Message = "The specified item does not have any bids",
                        MessageCode = MessageCode.Backend.SellLotNoCurrentBids
                    };
                }

                // Get the last highest bid.
                var lastBid = lotState.Value.Bids.OrderBy(i => i.BidDateTime).Last();

                // Ensure the correct lot status is applied before notifying the engine.
                lot.Value.Status = LotStatus.Sold;

                // Tell the auction controller that this lot is now sold and closed.
                await (await GetAuctionControllerProxy()).NotifyLotClosedAsync(lot.Value, lotState.Value, request.RequestId);

                // Clear the Actor's state to indicate that this item is no longer part of any open auction.
                await StateManager.RemoveStateAsync(nameof(AuctionLotState));
                await StateManager.RemoveStateAsync(nameof(AuctionLot));

                return new SellLotResponse
                {
                    Status = SaleStatus.Sold,
                    Success = true,
                    MessageCode = MessageCode.Backend.SellLotAckOnSuccess,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    AuctionId = lot.Value.AuctionId.ToUpperCase(),
                    UserId = lastBid.UserId,
                    ItemPrice = lotState.Value.CurrentPrice
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while selling {ItemId}", request.ItemId);

                return new SellLotResponse
                {
                    Status = SaleStatus.Invalid,
                    Success = false,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.SellLotServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<CancelLotResponse> CancelLotAsync(CancelLotRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(CancelLotAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        {
            try
            {
                // Read the current state of the lot owned by this actor.
                var lot = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

                // Make sure this request is for the correct lot instance.
                if (!lot.HasValue || lot.Value.ItemId != Guid.Parse(request.ItemId))
                {
                    return new CancelLotResponse
                    {
                        Status = SaleStatus.NotFound,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "The specified item is not part any open auction"
                    };
                }

                var lotState = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                // Make sure this request is for the correct lot state.
                if (!lotState.HasValue)
                {
                    return new CancelLotResponse
                    {
                        Status = SaleStatus.NotFound,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        AuctionId = lot.Value.AuctionId.ToUpperCase(),
                        Message = "Lot state is gone"
                    };
                }

                // Ensure the correct lot status is applied before notifying the engine.
                lot.Value.Status = LotStatus.Past;

                // Tell the auction controller that this lot is now canceled and closed.
                var result = await (await GetAuctionControllerProxy()).NotifyLotCanceledAsync(lot.Value, lotState.Value, request.RequestId);

                // Only clean up the actor's state when cancellation was successfully processed.
                if (result.Success)
                {
                    // Clear the Actor's state to indicate that this item is no longer part of any open auction.
                    await StateManager.RemoveStateAsync(nameof(AuctionLotState));
                    await StateManager.RemoveStateAsync(nameof(AuctionLot));
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while canceling {ItemId}", request.ItemId);

                return new CancelLotResponse
                {
                    Status = SaleStatus.Invalid,
                    Success = false,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    Message = ex.Message
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<UpdateLotResponse> UpdateLotAsync(UpdateLotRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(UpdateLotAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestData, request, true))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        {
            try
            {
                // Read the current state of the lot owned by this actor.
                var lotObject = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

                // Make sure this request is for the correct lot instance.
                if (!lotObject.HasValue || lotObject.Value.ItemId != Guid.Parse(request.ItemId))
                {
                    return new UpdateLotResponse
                    {
                        Status = ApiResponseStatus.NotFound,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        Message = "The specified item is not part any open auction"
                    };
                }

                var lotStateObject = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                // Make sure this request is for the correct lot state.
                if (!lotStateObject.HasValue)
                {
                    return new UpdateLotResponse
                    {
                        Status = ApiResponseStatus.NotFound,
                        Success = false,
                        ItemId = request.ItemId,
                        RequestId = request.RequestId,
                        AuctionId = lotObject.Value.AuctionId.ToUpperCase(),
                        Message = "Lot state is gone"
                    };
                }

                // Extract the object stored in the lot's state for optimal performance.
                var lot = lotObject.Value;
                var lotState = lotStateObject.Value;

                // Ensure the correct lot status is applied before notifying the engine.
                lot.Status = request.LotStatus ?? lot.Status;
                lot.ScheduledCloseDate = request.ScheduledCloseDate ?? lot.ScheduledCloseDate;
                lot.BidIncrement = request.BidIncrement ?? lot.BidIncrement;

                // Tell the auction controller that this lot has been updated by the actor.
                var result = await (await GetAuctionControllerProxy()).NotifyLotUpdatedAsync(lot, lotState, request);

                // Only proceed with state update if we managed to successfully update the lot's data in the database.
                if (result.Success)
                {
                    // Save the updated information back to the Actor's state.
                    await StateManager.AddOrUpdateStateAsync(nameof(AuctionLot), lot, (n, v) => lot);

                    // If a new closing date/time was request, we need to update the reminder.
                    if (lot.IsTimedAuction() && request.ScheduledCloseDate.HasValue)
                    {
                        // Update the reminder that will notify this item when it is about to be closed.
                        await RegisterReminderAsync(GlobalConsts.ReminderNames.ScheduledClose, null, request.ScheduledCloseDate.Value.Subtract(DateTime.UtcNow), TimeSpan.FromMilliseconds(-1));

                        logger.Debug("Item {ItemId} will now close at {ScheduledCloseDate}", lot.ItemId, lot.ScheduledCloseDate);
                    }

                    // If an update in the bid increment was requested, notify all clients about the revised next bid.
                    if (request.BidIncrement.HasValue)
                    {
                        // Grab the highest bid (if available).
                        var highBid = lotState.HighBid;

                        // Update the lot's last high bid which is stored in the reliable state so that public site can reflect the sale as quickly as possible.
                        if (highBid != null)
                        {
                            // Compose a data object describing the high bid.
                            var highBidInfo = new HighBidInfo
                            {
                                AuctionId = lot.AuctionId.ToUpperCase(),
                                ItemId = lot.ItemId.ToUpperCase(),
                                ItemStatus = lot.Status,
                                UserId = highBid.UserId,
                                UserName = highBid.UserName,
                                UserState = highBid.UserState,
                                UserPaddleNo = highBid.UserPaddleNo,
                                BidId = highBid.BidId,
                                BidAmount = highBid.BidAmount,
                                MaxBidAmount = highBid.MaxBidAmount,
                                NextBidAmount = lot.GetNextBidAmount(lotState.CurrentPrice),
                                BidDateTime = highBid.BidDateTime,
                                ScheduledCloseDate = lot.ScheduledCloseDate,
                                BidIncrement = request.BidIncrement
                            };

                            // Notify the controller service about a high bid that has just been updated.
                            await (await GetAuctionControllerProxy()).NotifyHighBidAsync(highBidInfo);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while updating {ItemId}", request.ItemId);

                return new UpdateLotResponse
                {
                    Status = ApiResponseStatus.Failure,
                    Success = false,
                    ItemId = request.ItemId,
                    RequestId = request.RequestId,
                    Message = ex.Message
                };
            }
        }
    }

    async Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(IRemindable.ReceiveReminderAsync).RemoveAsyncSuffix()))
        {
            try
            {
                // Handle high bid reminders.
                if (reminderName.Equals(GlobalConsts.ReminderNames.HighBid))
                {
                    var highBidInfo = serializer.Deserialize<HighBidInfo>(state);

                    using (logger.TimeOperation("Notifying on high bid for item {ItemId} with amount of {BidAmount}", highBidInfo.ItemId, highBidInfo.BidAmount))
                    {
                        // Read the actor's current state.
                        var lot = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);
                        var lotState = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                        // Only allow scheduled close date to be extended if it is actually defined at lot level, and is within the closing phase.
                        if (lot.HasValue && lot.Value.CanExtendScheduledCloseDate())
                        {
                            // Extend the lot's life time for extra 2 minutes.
                            lot.Value.ScheduledCloseDate = DateTimeOffset.UtcNow.Add(lot.Value.ClosingExtension ?? TimeSpan.Zero);

                            // Update the high bid information with the new expiration date/time now that it has changed.
                            highBidInfo.ScheduledCloseDate = lot.Value.ScheduledCloseDate;

                            // Save the updated information back to the Actor's state.
                            await StateManager.AddOrUpdateStateAsync(nameof(AuctionLot), lot.Value, (n, v) => lot.Value);

                            // Update the reminder that will notify this item when it is about to be closed.
                            await RegisterReminderAsync(GlobalConsts.ReminderNames.ScheduledClose, null, lot.Value.ScheduledCloseDate.Value.Subtract(DateTimeOffset.UtcNow), TimeSpan.FromMilliseconds(-1));

                            // Tell the auction controller that this lot's closing date/time has been extended by the actor.
                            await (await GetAuctionControllerProxy()).NotifyLotUpdatedAsync(lot.Value, lotState.Value, new UpdateLotRequest { ScheduledCloseDate = lot.Value.ScheduledCloseDate });

                            // Log a helpful message indicating that an update to the item's data has taken place.
                            logger.Debug("Item {ItemId} will now close at {ScheduledCloseDate}", lot.Value.ItemId, lot.Value.ScheduledCloseDate);
                        }

                        // Notify the controller service about a high bid that has just been accepted.
                        await (await GetAuctionControllerProxy()).NotifyHighBidAsync(highBidInfo);
                    }
                }
                // Handle scheduled close reminders.
                else if (reminderName.Equals(GlobalConsts.ReminderNames.ScheduledClose))
                {
                    using (logger.TimeOperation("Notifying on closed item {ItemId}", Id))
                    {
                        // Read all data from the actor's current state.
                        var lot = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);
                        var lotState = await StateManager.TryGetStateAsync<AuctionLotState>(nameof(AuctionLotState), cts.Token);

                        if (lot.HasValue && lotState.HasValue)
                        {
                            // Ensure the correct lot status is applied before notifying the engine.
                            lot.Value.Status = LotStatus.Sold;

                            // Tell the auction controller that this lot is now sold and closed.
                            await (await GetAuctionControllerProxy()).NotifyLotClosedAsync(lot.Value, lotState.Value, Guid.Empty.ToUpperCase());

                            // If the lot has any bids, we are going to need to update the last bid's status.
                            if (lotState.Value.Bids.Count > 0)
                            {
                                // Update the lot's last high bid which is stored in the reliable state so that public site can reflect the sale as quickly as possible.
                                var highBid = lotState.Value.Bids.Last();

                                // Compose a data object describing the high bid.
                                var highBidInfo = new HighBidInfo
                                {
                                    AuctionId = lot.Value.AuctionId.ToUpperCase(),
                                    ItemId = lot.Value.ItemId.ToUpperCase(),
                                    ItemStatus = lot.Value.Status,
                                    UserId = highBid.UserId,
                                    UserName = highBid.UserName,
                                    UserState = highBid.UserState,
                                    UserPaddleNo = highBid.UserPaddleNo,
                                    BidId = highBid.BidId,
                                    BidAmount = highBid.BidAmount,
                                    MaxBidAmount = highBid.MaxBidAmount,
                                    BidDateTime = highBid.BidDateTime,
                                    ScheduledCloseDate = lot.Value.ScheduledCloseDate,
                                    BidIncrement = lot.Value.BidIncrement
                                };

                                // Notify the controller service about a high bid that has just been updated.
                                await (await GetAuctionControllerProxy()).NotifyHighBidAsync(highBidInfo);
                            }

                            // Cleanup actor's state as this lot is no longer considered as active.
                            await StateManager.RemoveStateAsync(nameof(AuctionLot), cts.Token);
                            await StateManager.RemoveStateAsync(nameof(AuctionLotState), cts.Token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error has occurred while attempting to handle lot actor reminder {ReminderName}", reminderName);
            }
        }
    }

    private async Task<IAuctionController> GetAuctionControllerProxy()
    {
        // Use cached object (if available).
        if (auctionController != null) return auctionController;

        // If cached proxy object is gone, or was not as yet initialized, let's create a new one.
        // Read the current state of the lot owned by this actor.
        var lotObject = await StateManager.TryGetStateAsync<AuctionLot>(nameof(AuctionLot), cts.Token);

        if (lotObject.HasValue)
        {
            // Create and cache the service proxy object used for communication with Auction Controller.
            return auctionController = AppServiceProxy.Create<IAuctionController>(GlobalConsts.ServiceNames.AuctionController, lotObject.Value.AuctionId.ToUpperCase());
        }

        // If we can't read the actor's state, we should not allow the next operation to continue.
        throw new InvalidOperationException($"Lot actor was not properly initialized to service item {Id}");
    }
}
