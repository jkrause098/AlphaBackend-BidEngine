using Microsoft.ServiceFabric.Actors;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Domain;

namespace AuctionPlatform.BidsService.Interfaces;

/// <summary>
/// This interface defines the methods exposed by an actor.
/// Clients use this interface to interact with the actor that implements it.
/// </summary>
public interface ILotActor : IActor
{
    /// <summary>
    /// Open the specified <paramref name="lot"/> and activates bidding against it.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <param name="lot">The lot object.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<OpenLotResponse> OpenLotAsync(OpenLotRequest request, AuctionLot lot);

    /// <summary>
    /// Places a new bid for the lot specified in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<PlaceBidResponse> PlaceBidAsync(PlaceBidRequest request);

    /// <summary>
    /// Returns all bids for the lot specified in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<RetrieveBidsResponse> RetrieveBidsAsync(RetrieveBidsRequest request);

    /// <summary>
    /// Deletes the last bid placed for the lot specified in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<DeleteBidsResponse> DeleteBidsAsync(DeleteBidsRequest request);

    /// <summary>
    /// Marks the lot specified in the <paramref name="request"/> as sold.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<SellLotResponse> SellLotAsync(SellLotRequest request);

    /// <summary>
    /// Marks the lot specified in the <paramref name="request"/> as canceled and removes it from the open auction.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<CancelLotResponse> CancelLotAsync(CancelLotRequest request);

    /// <summary>
    /// Updates the specified lot with the new details provided in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object containing parameters that describe a request.</param>
    /// <returns>A response object indicating the outcome of the operation.</returns>
    Task<UpdateLotResponse> UpdateLotAsync(UpdateLotRequest request);
}
