using FluentValidation;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class AuctionInfoValidator : AbstractValidator<AuctionInfo>
{
    public AuctionInfoValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.AuctionNo).NotEmpty();
        RuleFor(x => x.BidIncrements).NotEmpty().WithMessage("Bid increments must contain one or more items");
        RuleFor(x => x.Lots).NotEmpty().WithMessage("Auction must contain one or more lots");
    }
}
