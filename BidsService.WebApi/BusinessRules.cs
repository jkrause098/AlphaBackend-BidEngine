namespace AuctionPlatform.BidsService.WebApi;

internal static class BusinessRules
{
    public static DateTimeOffset? ToUtc(this DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }
}
