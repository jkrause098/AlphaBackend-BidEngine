namespace AuctionPlatform.BidsService.ApiContracts
{
    /// <summary>
    /// Represents the current status of the lot in the auction.
    /// </summary>
    /// <remarks>
    /// This enumeration is mapped to the ItemBidStatus entity in the database.
    /// </remarks>
    public enum LotStatus
    {
        NoBid = 0,
        SystemBid = 1,
        LiveOnBlock = 2,
        InReview = 3,
        Sold = 4,
        Past = 5,
        Paused = 6
    }
}
