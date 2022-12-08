using FluentValidation;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain.Validators;

public class UpdateLotRequestValidator : AbstractValidator<UpdateLotRequest>
{
    public UpdateLotRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.ItemId).Must(v => Guid.TryParse(v, out _)).WithMessage("Item ID must be a GUID value");

        RuleFor(x => x.LotStatus).Must(v => v == LotStatus.LiveOnBlock || v == LotStatus.SystemBid)
                                 .When(x => x.LotStatus.HasValue)
                                 .WithMessage($"The lot's bidding status can be update to either {LotStatus.LiveOnBlock} or {LotStatus.SystemBid} using this API");

        RuleFor(x => x.ScheduledCloseDate).Must(v => v > DateTime.UtcNow)
                                          .When(x => x.ScheduledCloseDate.HasValue)
                                          .WithMessage("The lot's scheduled close date/time must not be in the past");

        RuleFor(x => x.BidIncrement).GreaterThan(0)
                                    .When(x => x.BidIncrement.HasValue)
                                    .WithMessage($"The bid increment must be greater than zero");
    }
}
