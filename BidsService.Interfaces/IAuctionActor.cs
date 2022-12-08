using Microsoft.ServiceFabric.Actors;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Domain;

namespace AuctionPlatform.BidsService.Interfaces;

/// <summary>
/// This interface defines the methods exposed by an actor.
/// Clients use this interface to interact with the actor that implements it.
/// </summary>
public interface IAuctionActor : IActor
{
    /// <summary>
    /// Performs a series of operations that prepare the specified auction for bidding.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <param name="auctionInfo">The object carrying auction information.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<OpenAuctionResponse> OpenAuctionAsync(OpenAuctionRequest request, AuctionInfo auctionInfo);

    /// <summary>
    /// Performs a series of operations that prepare the specified auction ring for bidding.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <param name="auctionInfo">The object carrying auction information.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<OpenRingResponse> OpenRingAsync(OpenRingRequest request, AuctionInfo auctionInfo);

    /// <summary>
    /// Updates the ring specified in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <param name="auctionInfo">The object carrying auction information.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<UpdateRingResponse> UpdateRingAsync(UpdateRingRequest request, AuctionInfo auctionInfo);

    /// <summary>
    /// Retrieves the details about the auction specified in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<GetAuctionInfoResponse> GetAuctionInfoAsync(GetAuctionInfoRequest request);

    /// <summary>
    /// Performs a series of operations that stop bidding and close the specified auction.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<CloseAuctionResponse> CloseAuctionAsync(CloseAuctionRequest request);

    /// <summary>
    /// Performs a series of operations that prepare the specified choice lot group for bidding.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <param name="lots">The list of lots associated with the choice lot group.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<OpenChoiceLotResponse> OpenChoiceLotAsync(OpenChoiceLotRequest request, AuctionLot[] lots);

    /// <summary>
    /// Performs a series of operations that process the specified choice lot group as sold.
    /// </summary>
    /// <param name="request">The object carrying request parameters.</param>
    /// <returns>The object carrying the outcome of this operation.</returns>
    Task<SellChoiceLotResponse> SellChoiceLotAsync(SellChoiceLotRequest request);
}
