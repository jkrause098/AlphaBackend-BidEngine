using Microsoft.ServiceFabric.Services.Remoting;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Domain;

namespace AuctionPlatform.BidsService.Interfaces;

/// <summary>
/// Defines a contract that must be supported by Auction Controller service.
/// </summary>
public interface IAuctionController : IService
{
    /// <summary>
    /// Notifies the service about a high bid that has been accepted.
    /// </summary>
    /// <param name="highBidEvent">The object carrying high bid details.</param>
    /// <returns>No result.</returns>
    Task NotifyHighBidAsync(HighBidInfo highBidEvent);

    /// <summary>
    /// Notifies the service about the closed auction lot.
    /// </summary>
    /// <param name="lot">The auction lot that has been closed.</param>
    /// <param name="lotState">The state that is associated with the closed lot.</param>
    /// <param name="requestId">The unique ID of the request assigned by the server when handling the client's request.</param>
    /// <returns>The response object.</returns>
    Task NotifyLotClosedAsync(AuctionLot lot, AuctionLotState lotState, string requestId);

    /// <summary>
    /// Notifies the service about the canceled auction lot.
    /// </summary>
    /// <param name="lot">The auction lot that has been canceled.</param>
    /// <param name="lotState">The state that is associated with the canceled lot.</param>
    /// <param name="requestId">The unique ID of the request assigned by the server when handling the client's request.</param>
    /// <returns>The response object.</returns>
    Task<CancelLotResponse> NotifyLotCanceledAsync(AuctionLot lot, AuctionLotState lotState, string requestId);

    /// <summary>
    /// Notifies the service about the updated auction lot.
    /// </summary>
    /// <param name="lot">The auction lot that has been updated.</param>
    /// <param name="lotState">The state that is associated with the updated lot.</param>
    /// <param name="request">The object containing parameters that describe the updates made to the lot.</param>
    /// <returns>The response object.</returns>
    Task<UpdateLotResponse> NotifyLotUpdatedAsync(AuctionLot lot, AuctionLotState lotState, UpdateLotRequest request);

    /// <summary>
    /// Returns the current high bids for the items specified in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <returns>The response object.</returns>
    Task<RetrieveHighBidsResponse> RetrieveHighBidsAsync(RetrieveHighBidsRequest request);

    /// <summary>
    /// Records the bid described by <paramref name="bidInfo"/> in the internal audit data store.
    /// </summary>
    /// <param name="request">The object containing parameters that describe the bid that was placed.</param>
    /// <param name="outcome">The object describing the outcome of placing the bid</param>
    /// <returns>No result.</returns>
    Task TrackBidAsync(PlaceBidRequest request, PlaceBidResponse outcome);

    /// <summary>
    /// Notifies the service about the closed auction.
    /// </summary>
    /// <param name="auction">The auction that has been closed.</param>
    /// <param name="requestId">The unique ID of the request assigned by the server when handling the client's request.</param>
    /// <returns>The response object.</returns>
    Task NotifyAuctionClosedAsync(AuctionInfo auction, string requestId);

    /// <summary>
    /// Notifies the service about a new choice lot group.
    /// </summary>
    /// <param name="choiceLot">The virtual lot representing choice lot group.</param>
    /// <param name="lots">The lots participating in the choice lot group.</param>
    /// <param name="requestId">The unique ID of the request assigned by the server when handling the client's request.</param>
    /// <returns>The response object.</returns>
    Task NotifyChoiceLotCreatedAsync(AuctionLot choiceLot, AuctionLot[] lots, string requestId);
}
