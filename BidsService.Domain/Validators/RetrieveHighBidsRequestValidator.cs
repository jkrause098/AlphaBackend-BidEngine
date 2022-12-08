using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class RetrieveHighBidsRequestValidator : AbstractValidator<RetrieveHighBidsRequest>
{
    public RetrieveHighBidsRequestValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.ItemIds).NotEmpty().WithMessage("Request must contain one or more item ID");
    }
}
