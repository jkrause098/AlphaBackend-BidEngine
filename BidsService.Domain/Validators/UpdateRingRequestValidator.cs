using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class UpdateRingRequestValidator : AbstractValidator<UpdateRingRequest>
{
    public UpdateRingRequestValidator()
    {
        RuleFor(x => x.RingId).NotEmpty();
        RuleFor(x => x.RingId).Must(v => Guid.TryParse(v, out _)).WithMessage("Ring ID must be a GUID value");
    }
}
