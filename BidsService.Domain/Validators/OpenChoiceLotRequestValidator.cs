using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class OpenChoiceLotRequestValidator : AbstractValidator<OpenChoiceLotRequest>
{
    public OpenChoiceLotRequestValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.AuctionId).Must(v => Guid.TryParse(v, out _)).WithMessage("Auction ID must be a GUID value");

        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.GroupId).Must(v => Guid.TryParse(v, out _)).WithMessage("Group ID must be a GUID value");
    }
}
