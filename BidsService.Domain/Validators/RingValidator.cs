using FluentValidation;
using AuctionPlatform.Domain.Entities;
using RingStatus = AuctionPlatform.BidsService.ApiContracts.RingStatus;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class RingValidator : AbstractValidator<Ring>
{
    public RingValidator()
    {
        RuleFor(x => x.RingStatusId).Must(v => v == (int)RingStatus.Closed).WithMessage("Ring must be closed before opening");
        RuleFor(x => x.IsBiddingEnabled).NotEmpty().WithMessage("Bidding must be enabled for this ring");
        RuleFor(x => x.Auction).NotEmpty().WithMessage("Ring must be associated with an auction");
    }
}
