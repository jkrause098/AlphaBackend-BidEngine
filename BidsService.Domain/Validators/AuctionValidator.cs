using FluentValidation;
using AuctionPlatform.Domain.Entities;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class AuctionValidator : AbstractValidator<Auction>
{
    public AuctionValidator()
    {
        RuleFor(x => x.BidIncrementTableId).NotEmpty();
        RuleFor(x => x.IsActive).NotEmpty().WithMessage("Auction must be set to active");
    }
}
