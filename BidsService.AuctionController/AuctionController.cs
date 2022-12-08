using System.Fabric;
using Azure.Messaging.ServiceBus;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Domain;
using AuctionPlatform.BidsService.Interfaces;
using SullivanAuctioneers.Common.Data;
using SullivanAuctioneers.Common.ServiceRuntime;
using AuctionPlatform.ResourceAccess.EntityFramework;

namespace AuctionPlatform.BidsService.AuctionController;

/// <summary>
/// An instance of this class is created for each service replica by the Service Fabric runtime.
/// </summary>
internal sealed class AuctionController : StatefulService, IAuctionController
{
    private readonly IConfiguration configuration;
    private readonly ILogger logger;
    private readonly IDbContextFactory<SaDbContext> dbContextFactory;
    private readonly ServiceManager messagingProxy;
    private readonly ServiceBusSender userEventSender;
    private readonly ServiceBusSender bidLogSender;
    private readonly ITransformer<PlaceBidRequest, PlaceBidResponse, BidLogEntry> bidLogTransformer;
    private readonly ITransformer<HighBidInfo, BidLogEntry> highBidToBidLogEntryTransformer;
    private readonly ITransformer<HighBidInfo, HighBidEvent> highBidEventTransform;
    private readonly ITransformer<LotUpdatedEvent[], BidInfo> bidInfoTransform;
    private readonly CancellationTokenSource cts = new();
    private IServiceHubContext? hubContext;

    public AuctionController(StatefulServiceContext context, IServiceProvider provider)
        : base(context)
    {
        logger = provider.GetRequiredService<ILogger>();
        configuration = provider.GetRequiredService<IConfiguration>();
        dbContextFactory = provider.GetRequiredService<IDbContextFactory<SaDbContext>>();
        messagingProxy = provider.GetRequiredService<ServiceManager>();
        bidLogTransformer = provider.GetRequiredService<ITransformer<PlaceBidRequest, PlaceBidResponse, BidLogEntry>>();
        highBidEventTransform = provider.GetRequiredService<ITransformer<HighBidInfo, HighBidEvent>>();
        highBidToBidLogEntryTransformer = provider.GetRequiredService<ITransformer<HighBidInfo, BidLogEntry>>();
        bidInfoTransform = provider.GetRequiredService<ITransformer<LotUpdatedEvent[], BidInfo>>();

        var sbClient = provider.GetRequiredService<ServiceBusClient>();
        userEventSender = sbClient.CreateSender(configuration[GlobalConsts.SettingNames.UserEventTopicName]);
        bidLogSender = sbClient.CreateSender(configuration[GlobalConsts.SettingNames.BidLogTopicName]);
    }

    /// <summary>
    /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
    /// </summary>
    /// <remarks>
    /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
    /// </remarks>
    /// <returns>A collection of listeners.</returns>
    protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners() => this.CreateServiceRemotingReplicaListeners();

    /// <summary>
    /// This is the main entry point for your service replica.
    /// This method executes when this replica of your service becomes primary and has write status.
    /// </summary>
    /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
    protected override Task RunAsync(CancellationToken cancellationToken)
    {
        // Broadcast cancellations to all tokens issued internally.
        cancellationToken.Register(() =>
        {
            cts.Cancel();
        });

        return Task.WhenAll(ProcessLotActorEvictionsAsync(cancellationToken), ProcessAuctionActorEvictionsAsync(cancellationToken));
    }

    /// <summary>
    /// This method is called when the replica is being opened and it is the final step
    /// of opening the service. Override this method to be notified that Open has completed
    /// for this replica's internal components.
    /// </summary>
    /// <param name="openMode">ReplicaOpenMode for this service replica.</param>
    /// <param name="cancellationToken">Cancellation token to monitor for cancellation requests.</param>
    /// <returns>A Task that represents outstanding operation.</returns>
    protected override async Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
    {
        // Create a messaging object that communicates with Azure SignalR.
        hubContext = await messagingProxy.CreateHubContextAsync("SABiddingHub", cancellationToken);
    }

    /// <summary>
    /// This method is called as the final step of closing the service gracefully. Override
    /// this method to be notified that Close has completed for this replica's internal components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to monitor for cancellation requests.</param>
    /// <returns>A Task that represents outstanding operation.</returns>
    protected override async Task OnCloseAsync(CancellationToken cancellationToken)
    {
        // Gracefully terminate the connection to Azure SignalR.
        if (hubContext != null)
        {
            await hubContext.DisposeAsync();
        }

        // Gracefully terminate all connections to Service Bus.
        await userEventSender.CloseAsync(cancellationToken);
        await bidLogSender.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task NotifyHighBidAsync(HighBidInfo highBidInfo)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(NotifyHighBidAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, highBidInfo.ItemId))
        {
            try
            {
                // Figure out what state to query and what item key to use.
                var stateKey = KeyUtils.GetHighBidKey(highBidInfo.ItemId);
                var stateStore = await StateManager.GetOrAddAsync<IReliableDictionary<string, HighBidInfo>>(nameof(HighBidInfo));

                // All updates in the service state must be subject to a transaction.
                using var tx = StateManager.CreateTransaction();

                // Update the state and save the updates back to the store.
                await stateStore.AddOrUpdateAsync(tx, stateKey, highBidInfo, (k, v) => highBidInfo);
                await tx.CommitAsync();

                // Send the current high bid to the Service Bus queue.
                await TrackBidAsync(highBidInfo, cts.Token);

                // Send the current high bid to all clients currently connected to Azure SignalR.
                if (hubContext != null)
                {
                    // We are going to need to apply additional transformation on events sent to SignalR to remove
                    // properties that should not visible to all other users.
                    var highBidEvent = highBidEventTransform.Transform(highBidInfo);

                    // Invokes a method on all Azure SignalR client connections.
                    await hubContext.Clients.All.SendAsync(GlobalConsts.EventNames.HighBidEvent, highBidEvent, cts.Token);
                }
            }
            catch (FabricNotPrimaryException)
            {
                logger.Warning("{ServiceName} partition ID {PartitionId} replica ID {ReplicaId} cannot serve {OperationName} requests", Context.ServiceName, Context.PartitionId, Context.ReplicaId, nameof(NotifyHighBidAsync));

                // We must re-throw FabricNotPrimaryException without handling (or even logging it as an error).
                // It will be caught by the service proxy which will retry the failed call against a freshly resolved replica.
                // This prevents from creating a stickiness to a wrong replica.
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to persist high bid for item {ItemId}", highBidInfo.ItemId);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyLotClosedAsync(AuctionLot lot, AuctionLotState lotState, string requestId)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(NotifyLotClosedAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, requestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, lot.ItemId))
        {
            try
            {
                // Create a new scoped instance of the database context object.
                var dbContext = await dbContextFactory.CreateDbContextAsync();

                // Get the lot entity from the primary database.
                var item = await dbContext.Items.Where(a => a.Id == lot.ItemId).FirstOrDefaultAsync();

                // Make sure the specified lot actually exists.
                if (item == null)
                {
                    logger.Error("The specified item {ItemId} was not found in the database", lot.ItemId);
                    return;
                };

                // Update item details upon closing.
                item.ActualCloseDate = DateTime.UtcNow;
                item.ItemBidStatusId = (int)lot.Status;

                // Update the item's details.
                dbContext.Items.Update(item);

                // Populate item's bid information.
                dbContext.BulkInsertOrUpdate(BusinessRules.CreateItemBids(lot, lotState.Bids).ToList());

                // Persist changes in the database.
                await dbContext.SaveChangesAsync(cts.Token);

                // Generate an event that will be sent to SignalR when bidding is closed for a given auction lot.
                var lotClosedEvent = new LotClosedEvent
                {
                    AuctionId = lot.AuctionId,
                    ItemId = lot.ItemId
                };

                // Update the event with high bid details (if present).
                if (lotState.HighBidAmount > 0)
                {
                    var highBid = lotState.Bids.Last();

                    lotClosedEvent.UserId = Guid.Parse(highBid.UserId);
                    lotClosedEvent.BidId = highBid.BidId;
                    lotClosedEvent.BidAmount = highBid.BidAmount;
                    lotClosedEvent.BidDateTime = highBid.BidDateTime;
                }

                // Send the event to all clients currently connected to Azure SignalR.
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync(GlobalConsts.EventNames.LotClosedEvent, lotClosedEvent, cts.Token);
                }

                // Publish a new event to the Service Bus to let the backend know that we have closed this lot.
                await SendEventAsync(GlobalConsts.EventNames.LotClosedEvent, lotClosedEvent, cts.Token);

                // Notify the Service Fabric infrastructure that this lot actor is to be removed.
                await ScheduleLotActorEvictionAsync(lot.ItemId, cts.Token);
            }
            catch (FabricNotPrimaryException)
            {
                logger.Warning("{ServiceName} partition ID {PartitionId} replica ID {ReplicaId} cannot serve {OperationName} requests", Context.ServiceName, Context.PartitionId, Context.ReplicaId, nameof(NotifyLotClosedAsync));

                // We must re-throw FabricNotPrimaryException without handling (or even logging it as an error).
                // It will be caught by the service proxy which will retry the failed call against a freshly resolved replica.
                // This prevents from creating a stickiness to a wrong replica.
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to process closure notification for item {ItemId}", lot.ItemId);
            }
        }
    }

    /// <inheritdoc />
    public async Task<CancelLotResponse> NotifyLotCanceledAsync(AuctionLot lot, AuctionLotState lotState, string requestId)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(NotifyLotCanceledAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, requestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, lot.ItemId))
        {
            try
            {
                // Create a new scoped instance of the database context object.
                var dbContext = await dbContextFactory.CreateDbContextAsync();

                // Get the lot entity from the primary database.
                var item = await dbContext.Items.Where(a => a.Id == lot.ItemId).FirstOrDefaultAsync();

                // Make sure the specified lot actually exists.
                if (item == null)
                {
                    return new CancelLotResponse
                    {
                        Status = SaleStatus.NotFound,
                        Success = true,
                        ItemId = lot.ItemId.ToUpperCase(),
                        AuctionId = lot.AuctionId.ToUpperCase(),
                        RequestId = requestId,
                        Message = "The specified lot was not found in the database"
                    };
                };

                // Update item details upon closing.
                item.ItemBidStatusId = (int)lot.Status;

                // Update the item's details.
                dbContext.Items.Update(item);

                // Populate item's bid information.
                if (lotState.Bids.Count > 0)
                {
                    dbContext.BulkInsertOrUpdate(BusinessRules.CreateItemBids(lot, lotState.Bids).ToList());
                }

                // Persist changes in the database.
                await dbContext.SaveChangesAsync(cts.Token);

                // Generate an event that will be sent to SignalR when lot is canceled.
                var lotCanceledEvent = new LotCanceledEvent
                {
                    AuctionId = lot.AuctionId,
                    ItemId = lot.ItemId
                };

                // Update the event with high bid details (if present).
                if (lotState.HighBidAmount > 0)
                {
                    var highBid = lotState.Bids.Last();

                    lotCanceledEvent.UserId = Guid.Parse(highBid.UserId);
                    lotCanceledEvent.BidAmount = highBid.BidAmount;
                    lotCanceledEvent.BidDateTime = highBid.BidDateTime;
                }

                // Send the event to all clients currently connected to Azure SignalR.
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync(GlobalConsts.EventNames.LotCanceledEvent, lotCanceledEvent, cts.Token);
                }

                // Publish a new event to the Service Bus to let the backend know that we have canceled this lot.
                await SendEventAsync(GlobalConsts.EventNames.LotCanceledEvent, lotCanceledEvent, cts.Token);

                // Notify the Service Fabric infrastructure that this lot actor is to be removed.
                await ScheduleLotActorEvictionAsync(lot.ItemId, cts.Token);

                return new CancelLotResponse
                {
                    Status = SaleStatus.Canceled,
                    Success = true,
                    ItemId = lot.ItemId.ToUpperCase(),
                    AuctionId = lot.AuctionId.ToUpperCase(),
                    RequestId = requestId,
                    UserId = lotCanceledEvent.UserId?.ToUpperCase(),
                    ItemPrice = lotCanceledEvent.BidAmount
                };
            }
            catch (FabricNotPrimaryException)
            {
                logger.Warning("{ServiceName} partition ID {PartitionId} replica ID {ReplicaId} cannot serve {OperationName} requests", Context.ServiceName, Context.PartitionId, Context.ReplicaId, nameof(NotifyLotCanceledAsync));

                // We must re-throw FabricNotPrimaryException without handling (or even logging it as an error).
                // It will be caught by the service proxy which will retry the failed call against a freshly resolved replica.
                // This prevents from creating a stickiness to a wrong replica.
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to process cancellation for item {ItemId}", lot.ItemId);

                return new CancelLotResponse
                {
                    Status = SaleStatus.Invalid,
                    Success = false,
                    ItemId = lot.ItemId.ToUpperCase(),
                    AuctionId = lot.AuctionId.ToUpperCase(),
                    RequestId = requestId,
                    Message = ex.Message
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<UpdateLotResponse> NotifyLotUpdatedAsync(AuctionLot lot, AuctionLotState lotState, UpdateLotRequest request)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(NotifyLotUpdatedAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        {
            try
            {
                // Create a new scoped instance of the database context object.
                var dbContext = await dbContextFactory.CreateDbContextAsync();

                // Get the lot entity from the primary database.
                var item = await dbContext.Items.Where(a => a.Id == lot.ItemId).FirstOrDefaultAsync();

                // Make sure the specified lot actually exists.
                if (item == null) return new UpdateLotResponse
                {
                    Status = ApiResponseStatus.NotFound,
                    Success = false,
                    ItemId = request.ItemId,
                    AuctionId = lot.AuctionId.ToUpperCase(),
                    RequestId = request.RequestId,
                    Message = "The specified item was not found in the database"
                };

                // Handle bidding status update.
                if (request.LotStatus.HasValue)
                {
                    item.ItemBidStatusId = (int)request.LotStatus.Value;
                }

                // Handle scheduled close date/time update.
                if (request.ScheduledCloseDate.HasValue)
                {
                    item.ScheduledCloseDate = request.ScheduledCloseDate.Value.UtcDateTime;
                }

                // Update the item's details.
                dbContext.Items.Update(item);

                // Persist changes in the database (if any).
                await dbContext.SaveChangesAsync(cts.Token);

                // Generate an event that will be sent to SignalR when lot is updated.
                var lotUpdatedEvent = new LotUpdatedEvent
                {
                    AuctionId = lot.AuctionId,
                    ItemId = lot.ItemId,
                    ItemStatus = request.LotStatus,
                    ScheduledCloseDate = request.ScheduledCloseDate,
                    BidIncrement = request.BidIncrement
                };

                // Send the event to all clients currently connected to Azure SignalR.
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync(GlobalConsts.EventNames.LotUpdatedEvent, lotUpdatedEvent, cts.Token);

                    // As per conversation with Murly, we need to raise a separate HighBidEvent even though
                    // there are no bids on this lot. This helps keep the public site clean from any business
                    // rules, and decouples presentation layer from business logic layer.
                    if (request.BidIncrement.HasValue && lotState.HighBid == default)
                    {
                        // Include next bid amount in the event's payload so that public side can render the correct amount to the user.
                        lotUpdatedEvent.NextBidAmount = request.BidIncrement.Value;

                        // Invokes a method on all Azure SignalR client connections.
                        await hubContext.Clients.All.SendAsync(GlobalConsts.EventNames.HighBidEvent, lotUpdatedEvent, cts.Token);
                    }
                }

                // Keep a record of all updates made to a particular lot.
                await TrackLotUpdatesAsync(lotUpdatedEvent, cts.Token);

                // Publish a new event to the Service Bus to let the backend know that we have updated this lot.
                await SendEventAsync(GlobalConsts.EventNames.LotUpdatedEvent, lotUpdatedEvent, cts.Token);

                // Log the event associated with lot data update.
                logger.Debug("Lot ID {ItemId} has been successfully updated", lot.ItemId);

                return new UpdateLotResponse
                {
                    Status = ApiResponseStatus.Success,
                    Success = true,
                    ItemId = request.ItemId,
                    AuctionId = lot.AuctionId.ToUpperCase(),
                    RequestId = request.RequestId
                };
            }
            catch (FabricNotPrimaryException)
            {
                logger.Warning("{ServiceName} partition ID {PartitionId} replica ID {ReplicaId} cannot serve {OperationName} requests", Context.ServiceName, Context.PartitionId, Context.ReplicaId, nameof(NotifyLotCanceledAsync));

                // We must re-throw FabricNotPrimaryException without handling (or even logging it as an error).
                // It will be caught by the service proxy which will retry the failed call against a freshly resolved replica.
                // This prevents from creating a stickiness to a wrong replica.
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to process update request for item {ItemId}", request.ItemId);

                return new UpdateLotResponse
                {
                    Status = ApiResponseStatus.Failure,
                    Success = false,
                    ItemId = request.ItemId,
                    AuctionId = lot.AuctionId.ToUpperCase(),
                    RequestId = request.RequestId,
                    Message = ex.Message
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<RetrieveHighBidsResponse> RetrieveHighBidsAsync(RetrieveHighBidsRequest request)
    {
        try
        {
            // Figure out what state to query.
            var bidStateStore = await StateManager.GetOrAddAsync<IReliableDictionary<string, HighBidInfo>>(nameof(HighBidInfo));
            var itemStateStore = await StateManager.GetOrAddAsync<IReliableDictionary<string, LotUpdatedEvent[]>>(nameof(LotUpdatedEvent));

            // All access to the state store must be subject to a transaction.
            using var tx = StateManager.CreateTransaction();

            // Create a collection of parallel tasks.
            var getHighBidTasks = request.ItemIds.Select(itemId =>
            {
                var stateKey = KeyUtils.GetHighBidKey(itemId);
                return bidStateStore.TryGetValueAsync(tx, stateKey);
            });

            // Attempt to execute all tasks in parallel.
            var result = await Task.WhenAll(getHighBidTasks);

            // Find out what items actually have their high bids.
            var itemsWithBids = result?.Where(i => i.HasValue).Select(v => v.Value.ItemId).ToList();

            // Use this array to store bid information records for items without any bids.
            IEnumerable<BidInfo>? deltaBids = null;

            // If the number of items with bids is not same as the number of items specified in the request, we need more information.
            if (itemsWithBids == null || itemsWithBids.Count != request.ItemIds.Length)
            {
                // Create another collection of parallel tasks to read item information for those items that don't have any bids.
                var getItemInfoTasks = request.ItemIds.Where(i => itemsWithBids == null || !itemsWithBids.Contains(i)).Select(itemId =>
                {
                    var stateKey = KeyUtils.GetLotUpdatedEventKey(itemId);
                    return itemStateStore.TryGetValueAsync(tx, stateKey);
                });

                // Attempt to execute all tasks in parallel.
                var getItemInfoResult = await Task.WhenAll(getItemInfoTasks);

                // Extract item information from update history.
                deltaBids = getItemInfoResult.Where(i => i.HasValue).Select(i => bidInfoTransform.Transform(i.Value));
            }

            // Combine high bids with a list of items that don't have any bids but have other updates like custom bid increments.
            var bids = result?.Where(i => i.HasValue).Select(v => v.Value).Select(bid => new BidInfo
            {
                ItemId = bid.ItemId,
                ItemStatus = (int)bid.ItemStatus,
                UserId = bid.UserId,
                UserName = bid.UserName,
                UserState = bid.UserState,
                UserPaddleNo = bid.UserPaddleNo,
                BidAmount = bid.BidAmount,
                MaxBidAmount = bid.MaxBidAmount,
                BidDateTime = bid.BidDateTime,
                NextBidAmount = bid.NextBidAmount,
                ScheduledCloseDate = bid.ScheduledCloseDate,
                BidIncrement = bid.BidIncrement
            }).Union(deltaBids ?? Enumerable.Empty<BidInfo>()).ToArray();

            // Compose and return the response object.
            return new RetrieveHighBidsResponse
            {
                Status = bids != null && bids.Any() ? ApiResponseStatus.Success : ApiResponseStatus.NotFound,
                Success = true,
                AuctionId = request.AuctionId,
                RequestId = request.RequestId,
                Bids = bids
            };
        }
        catch (FabricNotPrimaryException)
        {
            logger.Warning("{ServiceName} partition ID {PartitionId} replica ID {ReplicaId} cannot serve {OperationName} requests", Context.ServiceName, Context.PartitionId, Context.ReplicaId, nameof(RetrieveHighBidsAsync));

            // We must re-throw FabricNotPrimaryException without handling (or even logging it as an error).
            // It will be caught by the service proxy which will retry the failed call against a freshly resolved replica.
            // This prevents from creating a stickiness to a wrong replica.
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unable to retrieve high bids");

            return new RetrieveHighBidsResponse
            {
                Status = ApiResponseStatus.Failure,
                Success = false,
                AuctionId = request.AuctionId,
                RequestId = request.RequestId,
                Message = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task TrackBidAsync(PlaceBidRequest request, PlaceBidResponse outcome)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(TrackBidAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, request.RequestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.UserId, request.UserId))
        {
            // Combine the original request and its outcome into a single object for convenience.
            var bidLogEntry = bidLogTransformer.Transform(request, outcome);
            var message = new ServiceBusMessage(JsonConvert.SerializeObject(bidLogEntry));

            await bidLogSender.SendMessageAsync(message, cts.Token);
        }
    }

    /// <inheritdoc />
    public async Task NotifyAuctionClosedAsync(AuctionInfo auction, string requestId)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(NotifyAuctionClosedAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, requestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, auction.AuctionId))
        {
            if (auction.Lots != null && auction.Lots.Length > 0)
            {
                // Build a request to retrieve the high bids for all lots in the auction being closed.
                var highBidsRequest = new RetrieveHighBidsRequest
                {
                    AuctionId = auction.AuctionId.ToUpperCase(),
                    ItemIds = auction.Lots.Select(i => i.ItemId.ToUpperCase()).ToArray(),
                    RequestId = requestId
                };

                // Retrieve the high bids for all lots in the auction being closed.
                var highBidsResponse = await RetrieveHighBidsAsync(highBidsRequest);

                // If high bid retrieval was successful, we are going to need to perform additional work.
                if (highBidsResponse.Success)
                {
                    var lotsWithBids = highBidsResponse.Bids.Select(i => Guid.Parse(i.ItemId));

                    // We need to notify all lots with at least one bid that they are about to be closed.
                    await Task.WhenAll(lotsWithBids.Select(itemId =>
                    {
                        // Create a remoting proxy for the specified auction lot.
                        var lotActor = ActorProxy.Create<ILotActor>(new ActorId(itemId));

                        // Compose a request asking to cancel the specified auction lot.
                        var request = new CancelLotRequest
                        {
                            ItemId = itemId.ToUpperCase(),
                            RequestId = requestId
                        };

                        // Invoke the actor's operation.
                        return lotActor.CancelLotAsync(request);
                    }));
                }

                // Schedule all lot actors for eviction from the Service Fabric cluster.
                await ScheduleLotActorEvictionAsync(auction.Lots.Select(i => i.ItemId).ToArray(), cts.Token);
            }

            // Generate an event that will be sent to SignalR when bidding is closed for a given auction.
            var auctionClosedEvent = new AuctionClosedEvent
            {
                AuctionId = auction.AuctionId,
                AuctionNo = auction.AuctionNo,
                LotCount = auction.Lots != null ? auction.Lots.Length : 0
            };

            // Publish a new event to the Service Bus to let the backend know that we have closed this auction.
            await SendEventAsync(GlobalConsts.EventNames.AuctionClosedEvent, auctionClosedEvent, cts.Token);

            // Notify the Service Fabric infrastructure that this auction actor is to be removed.
            await ScheduleAuctionActorEvictionAsync(auction.AuctionId, cts.Token);
        }
    }

    /// <inheritdoc />
    public async Task NotifyChoiceLotCreatedAsync(AuctionLot choiceLot, AuctionLot[] lots, string requestId)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(NotifyChoiceLotCreatedAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestId, requestId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, choiceLot.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ChoiceLotGroupId, choiceLot.ItemId))
        {
            try
            {
                // Generate an event that will be sent to SignalR when bidding is closed for a given auction.
                var lotCreatedEvent = new ChoiceLotCreatedEvent
                {
                    EventDateTime = DateTimeOffset.UtcNow,
                    AuctionId = choiceLot.AuctionId,
                    GroupId = choiceLot.ItemId.ToUpperCase(),
                    LotCount = lots != null ? lots.Length : 0,
                    Lots = lots?.Select(i => new LotDetails
                    {
                        ItemId = i.ItemId,
                        ItemLotNo = i.ItemLotNo,
                        ScheduledCloseDate = i.ScheduledCloseDate,
                        Status = i.Status
                    }).ToArray()
                };

                // Send the event to all clients currently connected to Azure SignalR.
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync(GlobalConsts.EventNames.ChoiceLotCreatedEvent, lotCreatedEvent, cts.Token);
                }

                // Publish a new event to the Service Bus to let the backend know that a new choice lot group has been created.
                await SendEventAsync(GlobalConsts.EventNames.ChoiceLotCreatedEvent, lotCreatedEvent, cts.Token);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to process choice lot created notification for group {ChoiceLotGroupId}", choiceLot.ItemId);
            }
        }
    }

    private async Task<bool> SendEventAsync<T>(string eventType, T eventPayload, CancellationToken cancellationToken)
    {
        try
        {
            var message = new ServiceBusMessage(JsonConvert.SerializeObject(eventPayload));
            message.ApplicationProperties.Add("EventType", eventType);

            await userEventSender.SendMessageAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error occurred while publishing {EventType} event to the Service Bus", eventType);
            return false;
        }
    }

    private async Task TrackBidAsync(HighBidInfo highBidInfo, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(TrackBidAsync).RemoveAsyncSuffix()))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, highBidInfo.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, highBidInfo.ItemId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.UserId, highBidInfo.UserId))
        {
            var bidLogEntry = highBidToBidLogEntryTransformer.Transform(highBidInfo);
            var message = new ServiceBusMessage(JsonConvert.SerializeObject(bidLogEntry));

            await bidLogSender.SendMessageAsync(message, cancellationToken);
        }
    }

    private async Task TrackLotUpdatesAsync(LotUpdatedEvent lotUpdatedEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Figure out what state to query and what item key to use.
            var stateKey = KeyUtils.GetLotUpdatedEventKey(lotUpdatedEvent.ItemId);
            var stateStore = await StateManager.GetOrAddAsync<IReliableDictionary<string, LotUpdatedEvent[]>>(nameof(LotUpdatedEvent));

            // All updates in the service state must be subject to a transaction.
            using var tx = StateManager.CreateTransaction();

            // Update the state and save the updates back to the store.
            await stateStore.AddOrUpdateAsync(tx, stateKey, new[] { lotUpdatedEvent }, (k, v) => v.Append(lotUpdatedEvent).ToArray());
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error occurred while persisting lot update event for item {ItemId}", lotUpdatedEvent.ItemId);
        }
    }

    private async Task ScheduleLotActorEvictionAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var commandQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Guid>>(LocalConsts.CommandQueueNames.LotActorEvictionRequests);

        using var tx = StateManager.CreateTransaction();

        await commandQueue.EnqueueAsync(tx, itemId, cancellationToken);
        await tx.CommitAsync();
    }

    private async Task ScheduleLotActorEvictionAsync(Guid[] itemIds, CancellationToken cancellationToken)
    {
        var commandQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Guid>>(LocalConsts.CommandQueueNames.LotActorEvictionRequests);

        using var tx = StateManager.CreateTransaction();

        foreach (var itemId in itemIds)
        {
            await commandQueue.EnqueueAsync(tx, itemId, cancellationToken);
        }

        await tx.CommitAsync();
    }

    private async Task ScheduleAuctionActorEvictionAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var commandQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Guid>>(LocalConsts.CommandQueueNames.AuctionActorEvictionRequests);

        using var tx = StateManager.CreateTransaction();

        await commandQueue.EnqueueAsync(tx, auctionId, cancellationToken);
        await tx.CommitAsync();
    }

    private async Task ProcessLotActorEvictionsAsync(CancellationToken cancellationToken)
    {
        using var _ = LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(ProcessLotActorEvictionsAsync).RemoveAsyncSuffix());

        while (true)
        {
            try
            {
                var commandQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Guid>>(LocalConsts.CommandQueueNames.LotActorEvictionRequests);

                while (!cancellationToken.IsCancellationRequested)
                {
                    while (commandQueue.Count == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // If the queue does not have the threshold number of items, delay the task and check again
                        await Task.Delay(TimeSpan.FromMilliseconds(LocalConsts.EmptyQueueDelayMs), cancellationToken);
                    }

                    using var tx = StateManager.CreateTransaction();
                    var queueItem = await commandQueue.TryDequeueAsync(tx, cancellationToken);

                    if (queueItem.HasValue)
                    {
                        try
                        {
                            // Obtain actor's URI and ID.
                            var actorId = new ActorId(queueItem.Value);
                            var actorUri = new ServiceUriBuilder(GlobalConsts.ServiceNames.LotActorService);

                            // Create a remoting proxy for the specified auction lot (item).
                            var actorProxy = ActorServiceProxy.Create(actorUri.ToUri(), actorId);

                            // Notify the Service Fabric infrastructure that this actor is to be removed.
                            await actorProxy.DeleteActorAsync(actorId, CancellationToken.None);

                            // Log the event associated with actor's removal.
                            logger.Information("Lot actor ID {ItemId} has been successfully evicted", queueItem.Value);

                            // Complete the transaction and remove the current item from the queue.
                            await tx.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Unable to handle lot actor eviction");

                            // Abort the transaction
                            tx.Abort();
                        }
                    }
                }

                // Existing the primary loop.
                break;
            }
            catch (OperationCanceledException)
            {
                logger.Warning("The processing loop for lot actor eviction requests has been terminated forcibly");
                break;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while processing lot actor eviction requests");
            }
        }
    }

    private async Task ProcessAuctionActorEvictionsAsync(CancellationToken cancellationToken)
    {
        using var _ = LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(ProcessAuctionActorEvictionsAsync).RemoveAsyncSuffix());

        while (true)
        {
            try
            {
                var commandQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Guid>>(LocalConsts.CommandQueueNames.AuctionActorEvictionRequests);

                while (!cancellationToken.IsCancellationRequested)
                {
                    while (commandQueue.Count == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // If the queue does not have the threshold number of items, delay the task and check again
                        await Task.Delay(TimeSpan.FromMilliseconds(LocalConsts.EmptyQueueDelayMs), cancellationToken);
                    }

                    using var tx = StateManager.CreateTransaction();
                    var queueItem = await commandQueue.TryDequeueAsync(tx, cancellationToken);

                    if (queueItem.HasValue)
                    {
                        try
                        {
                            // Obtain actor's URI and ID.
                            var actorId = new ActorId(queueItem.Value);
                            var actorUri = new ServiceUriBuilder(GlobalConsts.ServiceNames.AuctionActorService);

                            // Create a remoting proxy for the specified auction.
                            var actorProxy = ActorServiceProxy.Create(actorUri.ToUri(), actorId);

                            // Notify the Service Fabric infrastructure that this actor is to be removed.
                            await actorProxy.DeleteActorAsync(actorId, CancellationToken.None);

                            // Log the event associated with actor's removal.
                            logger.Information("Auction actor ID {AuctionId} has been successfully evicted", queueItem.Value);

                            // Complete the transaction and remove the current item from the queue.
                            await tx.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Unable to handle auction actor eviction");

                            // Abort the transaction
                            tx.Abort();
                        }
                    }
                }

                // Existing the primary loop.
                break;
            }
            catch (OperationCanceledException)
            {
                logger.Warning("The processing loop for auction actor eviction requests has been terminated forcibly");
                break;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while processing auction actor eviction requests");
            }
        }
    }
}
