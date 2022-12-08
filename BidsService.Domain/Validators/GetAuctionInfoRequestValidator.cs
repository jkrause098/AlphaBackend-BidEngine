using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class GetAuctionInfoRequestValidator : AbstractValidator<GetAuctionInfoRequest>
{
    public GetAuctionInfoRequestValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.AuctionId).Must(v => Guid.TryParse(v, out _)).WithMessage("Auction ID must be a GUID value");
        RuleFor(x => x.RequestId).NotEmpty();
    }
}
