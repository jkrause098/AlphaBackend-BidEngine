using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class PlaceBidRequestValidator : AbstractValidator<PlaceBidRequest>
{
    public PlaceBidRequestValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.AuctionId).Must(v => Guid.TryParse(v, out _)).WithMessage("Auction ID must be a GUID value");

        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.ItemId).Must(v => Guid.TryParse(v, out _)).WithMessage("Item ID must be a GUID value");

        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserId).Must(v => Guid.TryParse(v, out _)).WithMessage("User ID must be a GUID value");

        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.UserState).NotEmpty();
        RuleFor(x => x.UserPaddleNo).NotEmpty();

        RuleFor(x => x.BidAmount).NotEmpty().When(x => !x.MaxBidAmount.HasValue);
        RuleFor(x => x.BidAmount).GreaterThan(0).When(x => !x.MaxBidAmount.HasValue);

        RuleFor(x => x.MaxBidAmount).NotEmpty().When(x => !x.BidAmount.HasValue);
        RuleFor(x => x.MaxBidAmount).GreaterThan(0).When(x => !x.BidAmount.HasValue);
    }
}
