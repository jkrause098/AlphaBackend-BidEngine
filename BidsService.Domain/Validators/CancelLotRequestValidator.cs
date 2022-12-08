using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class CancelLotRequestValidator : AbstractValidator<CancelLotRequest>
{
    public CancelLotRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.ItemId).Must(v => Guid.TryParse(v, out _)).WithMessage("Item ID must be a GUID value");
    }
}
