using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class SellChoiceLotRequestValidator : AbstractValidator<SellChoiceLotRequest>
{
    public SellChoiceLotRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.GroupId).Must(v => Guid.TryParse(v, out _)).WithMessage("Choice lot ID must be a GUID value");

        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserId).Must(v => Guid.TryParse(v, out _)).WithMessage("User ID must be a GUID value");

        RuleFor(x => x.ItemIds).NotEmpty().WithMessage("Request must contain one or more item ID");
        RuleForEach(x => x.ItemIds).Must(v => Guid.TryParse(v, out _)).WithMessage("Item ID must be a GUID value");
    }
}
