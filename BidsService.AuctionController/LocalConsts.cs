namespace AuctionPlatform.BidsService.AuctionController;

internal static class LocalConsts
{
    public const int EmptyQueueDelayMs = 5000;

    /// <summary>
    /// Consolidates all well-known command queue names in a single convenient location.
    /// </summary>
    public struct CommandQueueNames
    {
        public const string LotActorEvictionRequests = "LotActorEvictionRequests";
        public const string AuctionActorEvictionRequests = "AuctionActorEvictionRequests";
    }
}
