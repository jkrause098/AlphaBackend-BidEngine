using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Domain;
using AuctionPlatform.BidsService.Domain.Transforms;
using AuctionPlatform.BidsService.Domain.Validators;
using SullivanAuctioneers.Common.Data;
using DM = AuctionPlatform.Domain.Entities;

namespace AuctionPlatform.BidsService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainValidators(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<OpenAuctionRequest>, OpenAuctionRequestValidator>();
        services.AddSingleton<IValidator<CloseAuctionRequest>, CloseAuctionRequestValidator>();
        services.AddSingleton<IValidator<DM.Auction>, AuctionValidator>();
        services.AddSingleton<IValidator<AuctionInfo>, AuctionInfoValidator>();
        services.AddSingleton<IValidator<PlaceBidRequest>, PlaceBidRequestValidator>();
        services.AddSingleton<IValidator<RetrieveBidsRequest>, RetrieveLotBidsRequestValidator>();
        services.AddSingleton<IValidator<DeleteBidsRequest>, DeleteLotBidsRequestValidator>();
        services.AddSingleton<IValidator<RetrieveHighBidsRequest>, RetrieveHighBidsRequestValidator>();
        services.AddSingleton<IValidator<SellLotRequest>, SellLotRequestValidator>();
        services.AddSingleton<IValidator<CancelLotRequest>, CancelLotRequestValidator>();
        services.AddSingleton<IValidator<UpdateLotRequest>, UpdateLotRequestValidator>();
        services.AddSingleton<IValidator<OpenRingRequest>, OpenRingRequestValidator>();
        services.AddSingleton<IValidator<DM.Ring>, RingValidator>();
        services.AddSingleton<IValidator<UpdateRingRequest>, UpdateRingRequestValidator>();
        services.AddSingleton<IValidator<GetAuctionInfoRequest>, GetAuctionInfoRequestValidator>();
        services.AddSingleton<IValidator<OpenChoiceLotRequest>, OpenChoiceLotRequestValidator>();
        services.AddSingleton<IValidator<SellChoiceLotRequest>, SellChoiceLotRequestValidator>();

        return services;
    }

    public static IServiceCollection AddDomainTransforms(this IServiceCollection services)
    {
        services.AddSingleton<ITransformer<DM.BidIncrementTableDetail, BidIncrement>, BidIncrementTransform>();
        services.AddSingleton<ITransformer<PlaceBidRequest, PlaceBidResponse, BidLogEntry>, BidLogEntryTransform>();
        services.AddSingleton<ITransformer<HighBidInfo, BidLogEntry>, BidLogEntryTransform>();
        services.AddSingleton<ITransformer<HighBidInfo, HighBidEvent>, HighBidEventTransform>();
        services.AddSingleton<ITransformer<LotUpdatedEvent[], BidInfo>, BidInfoTransform>();

        return services;
    }
}
