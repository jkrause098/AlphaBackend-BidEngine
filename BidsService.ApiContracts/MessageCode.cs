namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Represents one of the potential codes for messages returned by the Bidding Engine.
/// </summary>
public static class MessageCode
{
    /// <summary>
    /// Defines codes for messages returned by front-end services (such as Web APIs).
    /// </summary>
    public struct Frontend
    {
        public const string OpenAuctionServerError = "FE:ERR:0000";
        public const string OpenAuctionInvalidRequest = "FE:ERR:0001";
        public const string OpenAuctionInvalidAuctionId = "FE:ERR:0002";
        public const string OpenAuctionInvalidData = "FE:ERR:0003";
        public const string GetAuctionInfoServerError = "FE:ERR:0004";
        public const string GetAuctionInfoInvalidRequest = "FE:ERR:0005";
        public const string CloseAuctionServerError = "FE:ERR:0006";
        public const string CloseAuctionInvalidRequest = "FE:ERR:0007";
        public const string PlaceBidServerError = "FE:ERR:0008";
        public const string PlaceBidInvalidRequest = "FE:ERR:0009";
        public const string RetrieveLotBidsInvalidRequest = "FE:ERR:00010";
        public const string RetrieveHighBidsInvalidRequest = "FE:ERR:00011";
        public const string DeleteLotBidsInvalidRequest = "FE:ERR:00012";
        public const string SellLotInvalidRequest = "FE:ERR:00013";
        public const string CancelLotInvalidRequest = "FE:ERR:00014";
        public const string UpdateLotInvalidRequest = "FE:ERR:00015";
        public const string OpenChoiceLotInvalidRequest = "FE:ERR:00016";
        public const string OpenChoiceLotGroupNotFound = "FE:ERR:00017";
        public const string OpenRingInvalidRequest = "FE:ERR:00018";
        public const string OpenRingNotFound = "FE:ERR:00019";
        public const string OpenRingInvalidData = "FE:ERR:00020";
        public const string OpenRingServerError = "FE:ERR:00021";
        public const string UpdateRingInvalidRequest = "FE:ERR:00022";
        public const string UpdateRingNotFound = "FE:ERR:00023";
        public const string SellChoiceLotInvalidRequest = "FE:ERR:00024";
        public const string SellChoiceLotUserNotHighBidder = "FE:ERR:00025";
    }

    /// <summary>
    /// Defines codes for messages returned by back-end services (such as ASF Actors).
    /// </summary>
    public struct Backend
    {
        public const string OpenAuctionAlreadyOpened = "BE:ERR:00001";
        public const string OpenAuctionAlreadyClosed = "BE:ERR:00002";
        public const string OpenAuctionAckOnSuccess = "BE:INF:00003";
        public const string OpenAuctionServerError = "BE:ERR:00004";
        public const string OpenRingAlreadyOpened = "BE:ERR:00005";
        public const string OpenRingAlreadyClosed = "BE:ERR:00006";
        public const string OpenRingAckOnSuccess = "BE:INF:00007";
        public const string OpenRingServerError = "BE:ERR:00008";
        public const string UpdateRingNotOpened = "BE:ERR:00009";
        public const string UpdateRingAckOnSuccess = "BE:INF:00010";
        public const string UpdateRingServerError = "BE:ERR:00011";
        public const string GetAuctionInfoAckOnSuccess = "BE:INF:00012";
        public const string GetAuctionInfoServerError = "BE:ERR:00013";
        public const string GetAuctionInfoNotOpened = "BE:INF:00014";
        public const string CloseAuctionAckOnSuccess = "BE:INF:00015";
        public const string CloseAuctionServerError = "BE:ERR:00016";
        public const string OpenChoiceLotNotOpened = "BE:ERR:00017";
        public const string OpenChoiceLotServerError = "BE:ERR:00018";
        public const string OpenLotAckOnSuccess = "BE:INF:00019";
        public const string OpenLotServerError = "BE:ERR:00020";
        public const string PlaceBidLotNotOpened = "BE:ERR:00021";
        public const string PlaceBidInvalidLotState = "BE:ERR:00022";
        public const string PlaceBidLotPaused = "BE:ERR:00023";
        public const string PlaceBidLowerThanMinimum = "BE:ERR:00024";
        public const string PlaceBidLowerThanHighest = "BE:ERR:00025";
        public const string PlaceBidWrongIncrement = "BE:ERR:00026";
        public const string PlaceBidAccepted = "BE:INF:00027";
        public const string PlaceBidOutbid = "BE:INF:00028";
        public const string PlaceBidServerError = "BE:ERR:00029";
        public const string RetrieveBidsInactiveLot = "BE:ERR:00030";
        public const string RetrieveBidsInvalidLotState = "BE:ERR:00031";
        public const string RetrieveBidsServerError = "BE:ERR:00032";
        public const string RetrieveBidsNoCurrentBids = "BE:INF:00033";
        public const string RetrieveBidsOneOrMoreFound = "BE:INF:00034";
        public const string DeleteBidsInactiveLot = "BE:ERR:00035";
        public const string DeleteBidsInvalidLotState = "BE:ERR:00036";
        public const string DeleteBidsServerError = "BE:ERR:00037";
        public const string DeleteBidsAckOnSuccess = "BE:INF:00038";
        public const string DeleteBidsNoCurrentBids = "BE:INF:00039";
        public const string SellLotInactiveLot = "BE:ERR:00040";
        public const string SellLotInvalidLotState = "BE:ERR:00041";
        public const string SellLotNoCurrentBids = "BE:INF:00042";
        public const string SellLotAckOnSuccess = "BE:INF:00043";
        public const string SellLotServerError = "BE:ERR:00044";
        public const string SellChoiceLotServerError = "BE:ERR:00045";
        public const string SellChoiceLotAckOnSuccess = "BE:INF:00046";
        public const string SellChoiceLotProcessError = "BE:ERR:00047";
    }
}
