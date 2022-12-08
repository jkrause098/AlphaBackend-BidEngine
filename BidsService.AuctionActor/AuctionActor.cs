using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using Serilog.Context;
using SerilogTimings.Extensions;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Domain;
using AuctionPlatform.BidsService.Infrastructure;
using AuctionPlatform.BidsService.Interfaces;
using AuctionPlatform.Domain.Entities;
using Azure.Core;

namespace AuctionPlatform.BidsService.AuctionActor;

/// <remarks>
/// Implements a resilient state-based Auction actor.
/// </remarks>
[StatePersistence(StatePersistence.Persisted)]
internal class AuctionActor : Actor, IAuctionActor
{
    private readonly IConfiguration configuration;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of AuctionActor
    /// </summary>
    /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
    /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
    public AuctionActor(ActorService actorService, ActorId actorId, IConfiguration configuration, ILogger logger)
        : base(actorService, actorId)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// This method is called whenever an actor is activated.
    /// An actor is activated the first time any of its methods are invoked.
    /// </summary>
    protected override Task OnActivateAsync()
    {
        logger.Debug("Auction actor {AuctionId} has been activated", Id);

        return StateManager.TryAddStateAsync(nameof(AuctionStatus), AuctionStatus.NotOpened);
    }

    /// <inheritdoc />
    public async Task<OpenAuctionResponse> OpenAuctionAsync(OpenAuctionRequest request, AuctionInfo auctionInfo)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenAuctionAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        {
            try
            {
                var auctionStatus = await StateManager.GetStateAsync<AuctionStatus>(nameof(AuctionStatus), CancellationToken.None);

                if (auctionStatus == AuctionStatus.Opened)
                {
                    return new OpenAuctionResponse
                    {
                        AuctionId = request.AuctionId,
                        Status = AuctionStatus.AlreadyOpened,
                        RequestId = request.RequestId,
                        Message = "Cannot perform this operation with the auction that is already opened",
                        MessageCode = MessageCode.Backend.OpenAuctionAlreadyOpened
                    };
                }

                if (auctionStatus == AuctionStatus.Closed)
                {
                    return new OpenAuctionResponse
                    {
                        AuctionId = request.AuctionId,
                        Status = AuctionStatus.AlreadyClosed,
                        RequestId = request.RequestId,
                        Message = "Cannot perform this operation with the auction that was previously closed",
                        MessageCode = MessageCode.Backend.OpenAuctionAlreadyClosed
                    };
                }

                // Spin off an actor for each item in this auction.
                await OpenAuctionLotsAsync(request.AuctionId, auctionInfo, request.RequestId);

                // Update auction state.
                await StateManager.AddOrUpdateStateAsync(nameof(AuctionStatus), (int)AuctionStatus.Opened, (key, value) => (int)AuctionStatus.Opened);
                await StateManager.AddOrUpdateStateAsync(nameof(AuctionInfo), auctionInfo, (key, value) => auctionInfo);

                // Return a success response.
                return new OpenAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    Status = AuctionStatus.Opened,
                    RequestId = request.RequestId,
                    Success = true,
                    Message = "The auction is now ready to accept bids",
                    MessageCode = MessageCode.Backend.OpenAuctionAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to open the specified auction {AuctionId}", request.AuctionId);

                return new OpenAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    Status = AuctionStatus.NotOpened,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.OpenAuctionServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<OpenRingResponse> OpenRingAsync(OpenRingRequest request, AuctionInfo auctionInfo)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenRingAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RingId, request.RingId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, auctionInfo.AuctionId))
        {
            try
            {
                var stateKeyName = KeyUtils.GetRingStateKey(request.RingId);
                var auctionStatus = await StateManager.TryGetStateAsync<AuctionStatus>(stateKeyName, CancellationToken.None);

                if (auctionStatus.HasValue && auctionStatus.Value == AuctionStatus.Opened)
                {
                    return new OpenRingResponse
                    {
                        AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                        RingId = request.RingId,
                        Status = AuctionStatus.AlreadyOpened,
                        RequestId = request.RequestId,
                        Message = "Cannot perform this operation with the ring that is already opened",
                        MessageCode = MessageCode.Backend.OpenRingAlreadyOpened
                    };
                }

                if (auctionStatus.HasValue && auctionStatus.Value == AuctionStatus.Closed)
                {
                    return new OpenRingResponse
                    {
                        AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                        RingId = request.RingId,
                        Status = AuctionStatus.AlreadyClosed,
                        RequestId = request.RequestId,
                        Message = "Cannot perform this operation with the ring that was previously closed",
                        MessageCode = MessageCode.Backend.OpenRingAlreadyClosed
                    };
                }

                // Spin off an actor for each item in this auction.
                await OpenAuctionLotsAsync(auctionInfo.AuctionId.ToUpperCase(), auctionInfo, request.RequestId);

                // Update auction state.
                await StateManager.AddOrUpdateStateAsync(stateKeyName, (int)AuctionStatus.Opened, (key, value) => (int)AuctionStatus.Opened);

                // Return a success response.
                return new OpenRingResponse
                {
                    AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                    RingId = request.RingId,
                    Status = AuctionStatus.Opened,
                    RequestId = request.RequestId,
                    Success = true,
                    Message = "The auction ring is now ready to accept bids",
                    MessageCode = MessageCode.Backend.OpenRingAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to open the specified ring {RingId}", request.RingId);

                return new OpenRingResponse
                {
                    AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                    RingId = request.RingId,
                    Status = AuctionStatus.NotOpened,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.OpenRingServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<UpdateRingResponse> UpdateRingAsync(UpdateRingRequest request, AuctionInfo auctionInfo)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(UpdateRingAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RingId, request.RingId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, auctionInfo.AuctionId))
        {
            try
            {
                var stateKeyName = KeyUtils.GetRingStateKey(request.RingId);
                var auctionStatus = await StateManager.TryGetStateAsync<AuctionStatus>(stateKeyName, CancellationToken.None);

                if (!auctionStatus.HasValue || auctionStatus.Value != AuctionStatus.Opened)
                {
                    return new UpdateRingResponse
                    {
                        AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                        RingId = request.RingId,
                        Status = ApiResponseStatus.Failure,
                        RequestId = request.RequestId,
                        Message = "Ring cannot be updated for an auction that is not currently open",
                        MessageCode = MessageCode.Backend.UpdateRingNotOpened
                    };
                }

                // Spin off an actor for each item in this ring and update their details.
                await UpdateAuctionLotsAsync(auctionInfo, request.RequestId);

                // Return a success response.
                return new UpdateRingResponse
                {
                    AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                    RingId = request.RingId,
                    Status = ApiResponseStatus.Success,
                    RequestId = request.RequestId,
                    Success = true,
                    Message = "Ring has been updated successfully",
                    MessageCode = MessageCode.Backend.UpdateRingAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to update the specified ring {RingId}", request.RingId);

                return new UpdateRingResponse
                {
                    AuctionId = auctionInfo.AuctionId.ToUpperCase(),
                    RingId = request.RingId,
                    Status = ApiResponseStatus.Failure,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.UpdateRingServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<GetAuctionInfoResponse> GetAuctionInfoAsync(GetAuctionInfoRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(GetAuctionInfoAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        {
            try
            {
                var auctionStatus = await StateManager.GetStateAsync<AuctionStatus>(nameof(AuctionStatus), CancellationToken.None);

                // The opened auction contains additional information that can be returned.
                if (auctionStatus == AuctionStatus.Opened)
                {
                    var auctionInfo = await StateManager.GetStateAsync<AuctionInfo>(nameof(AuctionInfo), CancellationToken.None);

                    return new GetAuctionInfoResponse
                    {
                        AuctionId = request.AuctionId,
                        AuctionNo = auctionInfo.AuctionNo,
                        Status = auctionStatus,
                        RequestId = request.RequestId,
                        Success = true,
                        MessageCode = MessageCode.Backend.GetAuctionInfoAckOnSuccess,
                        BidIncrements = auctionInfo.BidIncrements.Select(i => new BidIncrement
                        {
                            Low = i.Low,
                            High = i.High,
                            Increment = i.Increment
                        }).ToArray(),
                        Lots = auctionInfo.Lots.Select(i => new LotDetails
                        {
                            ItemId = i.ItemId,
                            ItemLotNo = i.ItemLotNo,
                            ScheduledCloseDate = i.ScheduledCloseDate,
                            Status = i.Status
                        }).ToArray()
                    };
                }

                return new GetAuctionInfoResponse
                {
                    AuctionId = request.AuctionId,
                    Status = auctionStatus,
                    RequestId = request.RequestId,
                    Success = true,
                    MessageCode = MessageCode.Backend.GetAuctionInfoNotOpened
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while retrieving auction details");

                return new GetAuctionInfoResponse
                {
                    AuctionId = request.AuctionId,
                    Status = AuctionStatus.NotOpened,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.GetAuctionInfoServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<CloseAuctionResponse> CloseAuctionAsync(CloseAuctionRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(CloseAuctionAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        {
            try
            {
                // Retrieve the current state of the auction.
                var auctionStatus = await StateManager.GetStateAsync<AuctionStatus>(nameof(AuctionStatus), CancellationToken.None);
                var auctionInfo = await StateManager.TryGetStateAsync<AuctionInfo>(nameof(AuctionInfo), CancellationToken.None);

                // Create and cache the service proxy object used for communication with Auction Controller.
                var auctionController = AppServiceProxy.Create<IAuctionController>(GlobalConsts.ServiceNames.AuctionController, request.AuctionId.ToUpperInvariant());

                // Tell the auction controller that this auction is now closed.
                await auctionController.NotifyAuctionClosedAsync(auctionInfo.HasValue ? auctionInfo.Value : new AuctionInfo { AuctionId = Guid.Parse(request.AuctionId) }, request.RequestId);

                // Clear the Actor's state to indicate that this item is no longer part of any open auction.
                await StateManager.RemoveStateAsync(nameof(AuctionStatus));
                await StateManager.TryRemoveStateAsync(nameof(AuctionInfo));

                return new CloseAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    Status = auctionStatus == AuctionStatus.Closed ? AuctionStatus.AlreadyClosed : AuctionStatus.Closed,
                    Success = true,
                    RequestId = request.RequestId,
                    MessageCode = MessageCode.Backend.CloseAuctionAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while closing the specified auction");

                return new CloseAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    Status = AuctionStatus.NotOpened,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.CloseAuctionServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<OpenChoiceLotResponse> OpenChoiceLotAsync(OpenChoiceLotRequest request, AuctionLot[] lots)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenChoiceLotAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ChoiceLotGroupId, request.GroupId))
        {
            try
            {
                // Retrieve the current state of the auction.
                var auctionStatus = await StateManager.GetStateAsync<AuctionStatus>(nameof(AuctionStatus), CancellationToken.None);
                var auctionInfo = await StateManager.TryGetStateAsync<AuctionInfo>(nameof(AuctionInfo), CancellationToken.None);

                // Do not allow calling this operation in a closed auction.
                if (auctionStatus != AuctionStatus.Opened || !auctionInfo.HasValue)
                {
                    return new OpenChoiceLotResponse
                    {
                        AuctionId = request.AuctionId,
                        GroupId = request.GroupId,
                        RequestId = request.RequestId,
                        Status = ApiResponseStatus.Failure,
                        Message = "Cannot perform this operation with an auction that is not opened",
                        MessageCode = MessageCode.Backend.OpenChoiceLotNotOpened
                    };
                }

                // Create a request message that contains parameters for the lot actor.
                var openLotRequest = new OpenLotRequest
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId
                };

                // Create a virtual lot which represents a choice lot group.
                var virtualLot = new AuctionLot
                {
                    AuctionId = lots.First().AuctionId,
                    AuctionType = lots.First().AuctionType,
                    ItemId = Guid.Parse(request.GroupId),
                    ItemLotNo = DateTime.UtcNow.Ticks.ToString(),
                    SellerId = Guid.Empty,
                    BidIncrements = auctionInfo.Value.BidIncrements,
                    Status = LotStatus.NoBid
                };

                // Create a remoting proxy for a virtual lot which represents a choice lot group.
                var lotActor = ActorProxy.Create<ILotActor>(new ActorId(virtualLot.ItemId));

                // Open the specified lot and activates bidding against it.
                var result = await lotActor.OpenLotAsync(openLotRequest, virtualLot);

                // Handle successful creation of a virtual choice lot.
                if (result.Success)
                {
                    // Advise all lots that are part of this group that they are now paused for bidding.
                    var updateLotTasks = lots.Select(i =>
                    {
                        // Create a remoting proxy for the specified auction lot.
                        var lotActor = ActorProxy.Create<ILotActor>(new ActorId(i.ItemId));

                        // Compose a request asking to update the lot's status.
                        var updateRequest = new UpdateLotRequest
                        {
                            ItemId = i.ItemId.ToUpperCase(),
                            LotStatus = LotStatus.Paused,
                            RequestId = request.RequestId
                        };

                        // Invoke the actor's operation.
                        return lotActor.UpdateLotAsync(updateRequest);
                    });

                    // Execute updates across multiple lots in parallel.
                    await Task.WhenAll(updateLotTasks);

                    // Create and cache the service proxy object used for communication with Auction Controller.
                    var auctionController = AppServiceProxy.Create<IAuctionController>(GlobalConsts.ServiceNames.AuctionController, request.AuctionId.ToUpperInvariant());

                    // Create a request for high bids.
                    var getHighBidsRequest = new RetrieveHighBidsRequest
                    {
                        AuctionId = request.AuctionId,
                        RequestId = request.RequestId,
                        ItemIds = lots.Select(i => i.ItemId.ToUpperCase()).ToArray()
                    };

                    // Invoke the remoting service operation.
                    var getHighBidsResponse = await auctionController.RetrieveHighBidsAsync(getHighBidsRequest);

                    // If items have any bids, we will need to do some additional work.
                    if (getHighBidsResponse.Success && getHighBidsResponse.Bids != null && getHighBidsResponse.Bids.Length > 0)
                    {
                        // Figure out the highest bid among all items in the choice log group.
                        var highBid = getHighBidsResponse.Bids.OrderByDescending(i => i.BidAmount).First();

                        // Create a request to place the initial bid against the choice lot.
                        var placeBidRequest = new PlaceBidRequest
                        {
                            AuctionId = request.AuctionId,
                            ItemId = virtualLot.ItemId.ToUpperCase(),
                            UserId = highBid.UserId,
                            UserName = highBid.UserName,
                            UserState = highBid.UserState,
                            UserPaddleNo = highBid.UserPaddleNo,
                            BidAmount = highBid.BidAmount,
                            RequestId = request.RequestId
                        };

                        // Invoke the actor's operation.
                        var placeBidResponse = await lotActor.PlaceBidAsync(placeBidRequest);

                        // If we failed to place the initial bid, we shall not proceed.
                        if (!placeBidResponse.Success)
                        {
                            return new OpenChoiceLotResponse
                            {
                                AuctionId = request.AuctionId,
                                GroupId = request.GroupId,
                                RequestId = request.RequestId,
                                Status = ApiResponseStatus.Failure,
                                Message = placeBidResponse.Message,
                                MessageCode = placeBidResponse.MessageCode,
                                Errors = placeBidResponse.Errors
                            };
                        }

                        // If the highest bid has been the result of a max bid, we will need to record the max bid as well.
                        if (highBid.MaxBidAmount.HasValue)
                        {
                            // Create a request to place the original max bid against the choice lot.
                            placeBidRequest = new PlaceBidRequest
                            {
                                AuctionId = request.AuctionId,
                                ItemId = virtualLot.ItemId.ToUpperCase(),
                                UserId = highBid.UserId,
                                UserName = highBid.UserName,
                                UserState = highBid.UserState,
                                UserPaddleNo = highBid.UserPaddleNo,
                                MaxBidAmount = highBid.MaxBidAmount,
                                RequestId = request.RequestId
                            };

                            // Invoke the actor's operation.
                            placeBidResponse = await lotActor.PlaceBidAsync(placeBidRequest);

                            // If we failed to place the max bid, we shall not proceed.
                            if (!placeBidResponse.Success)
                            {
                                return new OpenChoiceLotResponse
                                {
                                    AuctionId = request.AuctionId,
                                    GroupId = request.GroupId,
                                    RequestId = request.RequestId,
                                    Status = ApiResponseStatus.Failure,
                                    Message = placeBidResponse.Message,
                                    MessageCode = placeBidResponse.MessageCode,
                                    Errors = placeBidResponse.Errors
                                };
                            }
                        }

                        // Find a max bid that might have be placed in some other lot before it joined the group.
                        var maxBid = getHighBidsResponse.Bids.Where(i => i.MaxBidAmount.HasValue && i.MaxBidAmount > highBid.BidAmount).OrderByDescending(i => i.MaxBidAmount).FirstOrDefault();

                        // If a competing max bid is found, we will need to replay it against the choice lot so that it will adjust the highest bid correctly.
                        if (maxBid != null && (maxBid.UserId != highBid.UserId || (highBid.MaxBidAmount.HasValue && maxBid.MaxBidAmount > highBid.MaxBidAmount)))
                        {
                            // Create a request to place the highest max bid against the choice lot.
                            placeBidRequest = new PlaceBidRequest
                            {
                                AuctionId = request.AuctionId,
                                ItemId = virtualLot.ItemId.ToUpperCase(),
                                UserId = maxBid.UserId,
                                UserName = maxBid.UserName,
                                UserState = maxBid.UserState,
                                UserPaddleNo = maxBid.UserPaddleNo,
                                MaxBidAmount = maxBid.MaxBidAmount,
                                RequestId = request.RequestId
                            };

                            // Invoke the actor's operation.
                            placeBidResponse = await lotActor.PlaceBidAsync(placeBidRequest);

                            // If we failed to place the highest max bid, we shall not proceed.
                            if (!placeBidResponse.Success)
                            {
                                return new OpenChoiceLotResponse
                                {
                                    AuctionId = request.AuctionId,
                                    GroupId = request.GroupId,
                                    RequestId = request.RequestId,
                                    Status = ApiResponseStatus.Failure,
                                    Message = placeBidResponse.Message,
                                    MessageCode = placeBidResponse.MessageCode,
                                    Errors = placeBidResponse.Errors
                                };
                            }
                        }
                    }

                    // Tell the auction controller that a new choice lot group has been created.
                    await auctionController.NotifyChoiceLotCreatedAsync(virtualLot, lots, request.RequestId);
                }

                // Return the result of this operation.
                return new OpenChoiceLotResponse
                {
                    AuctionId = request.AuctionId,
                    GroupId = request.GroupId,
                    RequestId = request.RequestId,
                    Status = result.Status,
                    Success = result.Success,
                    Message = result.Message,
                    MessageCode = result.MessageCode,
                    Errors = result.Errors,
                    Lots = lots.Select(i => new LotDetails
                    {
                        ItemId = i.ItemId,
                        ItemLotNo = i.ItemLotNo,
                        ScheduledCloseDate = i.ScheduledCloseDate,
                        Status = i.Status
                    }).ToArray()
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while opening the specified choice lot");

                return new OpenChoiceLotResponse
                {
                    AuctionId = request.AuctionId,
                    GroupId = request.GroupId,
                    RequestId = request.RequestId,
                    Status = ApiResponseStatus.Failure,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.OpenChoiceLotServerError
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<SellChoiceLotResponse> SellChoiceLotAsync(SellChoiceLotRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(SellChoiceLotAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ChoiceLotGroupId, request.GroupId))
        {
            try
            {
                // Mark all items that are included into choice lot as sold.
                var sellResults = await Task.WhenAll(request.ItemIds.Select(itemId =>
                {
                    return SellLotAsync(request, itemId);
                }));

                // Handle any failures that may occur during batch operation with items.
                if (sellResults.Any(r => !r.Success))
                {
                    // Find the first failure that caused error condition.
                    var failure = sellResults.First(r => !r.Success);

                    // Return a fault response.
                    return new SellChoiceLotResponse
                    {
                        GroupId = request.GroupId,
                        RequestId = request.RequestId,
                        AuctionId = request.AuctionId,
                        UserId = request.UserId,
                        ItemId = failure.ItemId,
                        Status = failure.Status,
                        Message = failure.Message,
                        MessageCode = failure.MessageCode
                    };
                }

                // Return a success response.
                return new SellChoiceLotResponse
                {
                    GroupId = request.GroupId,
                    RequestId = request.RequestId,
                    AuctionId = request.AuctionId,
                    UserId = request.UserId,
                    ItemId = string.Join(';', request.ItemIds),
                    Status = SaleStatus.Sold,
                    Message = "The choice lot has been sold",
                    MessageCode = MessageCode.Backend.SellChoiceLotAckOnSuccess
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while selling the specified choice lot");

                return new SellChoiceLotResponse
                {
                    GroupId = request.GroupId,
                    RequestId = request.RequestId,
                    AuctionId = request.AuctionId,
                    UserId = request.UserId,
                    Status = SaleStatus.Invalid,
                    Message = ex.Message,
                    MessageCode = MessageCode.Backend.SellChoiceLotServerError
                };
            }
        }
    }

    private async Task<SellLotResponse> SellLotAsync(SellChoiceLotRequest request, string itemId)
    {
        try
        {
            // Create a remoting proxy for the specified auction lot.
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(itemId)));

            // Create a request to change the item's status to sold.
            var sellRequest = new SellLotRequest
            {
                ItemId = itemId,
                RequestId = request.RequestId
            };

            // Mark the item as sold.
            return await lotActor.SellLotAsync(sellRequest);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error occurred while handling the sale of the specified choice lot");

            return new SellChoiceLotResponse
            {
                GroupId = request.GroupId,
                RequestId = request.RequestId,
                AuctionId = request.AuctionId,
                UserId = request.UserId,
                Status = SaleStatus.Invalid,
                Message = ex.Message,
                MessageCode = MessageCode.Backend.SellChoiceLotProcessError
            };
        }
    }

    private async Task OpenAuctionLotsAsync(string auctionId, AuctionInfo auctionInfo, string requestId)
    {
        using (logger.TimeOperation("Opening {ItemCount} lot(s)", auctionInfo.Lots.Length))
        {
            await Task.WhenAll(auctionInfo.Lots.Select(lot =>
            {
                // Create a remoting proxy for the specified auction lot.
                var lotActor = ActorProxy.Create<ILotActor>(new ActorId(lot.ItemId));

                var request = new OpenLotRequest
                {
                    AuctionId = auctionId,
                    RequestId = requestId
                };

                // Must include other lot-specific configuration that is defined at auction level.
                if (lot.BidIncrements == null)
                {
                    lot.BidIncrements = auctionInfo.BidIncrements;
                }

                return lotActor.OpenLotAsync(request, lot);
            }));
        }
    }

    private async Task UpdateAuctionLotsAsync(AuctionInfo auctionInfo, string requestId)
    {
        using (logger.TimeOperation("Updating {ItemCount} lot(s)", auctionInfo.Lots.Length))
        {
            await Task.WhenAll(auctionInfo.Lots.Select(lot =>
            {
                // Create a remoting proxy for the specified auction lot.
                var lotActor = ActorProxy.Create<ILotActor>(new ActorId(lot.ItemId));

                // Compose a request asking to update the scheduled closing date/time for this item.
                var request = new UpdateLotRequest
                {
                    ItemId = lot.ItemId.ToUpperCase(),
                    ScheduledCloseDate = lot.ScheduledCloseDate,
                    RequestId = requestId
                };

                // Invoke the actor's operation.
                return lotActor.UpdateLotAsync(request);
            }));
        }
    }

    private async Task CancelAuctionLotsAsync(AuctionInfo auctionInfo, string requestId)
    {
        using (logger.TimeOperation("Canceling {ItemCount} lot(s)", auctionInfo.Lots.Length))
        {
            await Task.WhenAll(auctionInfo.Lots.Select(lot =>
            {
                // Create a remoting proxy for the specified auction lot.
                var lotActor = ActorProxy.Create<ILotActor>(new ActorId(lot.ItemId));

                // Compose a request asking to cancel the specified auction lot.
                var request = new CancelLotRequest
                {
                    ItemId = lot.ItemId.ToUpperCase(),
                    RequestId = requestId
                };

                // Invoke the actor's operation.
                return lotActor.CancelLotAsync(request);
            }));
        }
    }
}
