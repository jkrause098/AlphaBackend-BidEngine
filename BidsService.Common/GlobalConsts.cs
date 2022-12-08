namespace AuctionPlatform.BidsService.Common;

/// <summary>
/// Provides a set of reusable constants that need to be accessible globally throughout the solution.
/// </summary>
public static class GlobalConsts
{
    public const string AppEnvironmentVariable = "NETCORE_ENVIRONMENT";
    public const string CustomDevelopmentEnvironmentName = "dev-os";

    /// <summary>
    /// Consolidates all well-known setting names in a single convenient location.
    /// </summary>
    public struct SettingNames
    {
        public const string SqlConnectionString = "SqlServer--BidsService--ConnectionString";
        public const string SignalRConnectionString = "SignalRConfig--ConnectionString";
        public const string SignalRConnectionStringLocal = "SignalRConfig--ConnectionString--Local";
        public const string ServiceBusSenderConnectionString = "ServiceBusConfig--GlobalSenderPolicy--ConnectionString";
        public const string UserEventTopicName = "ServiceBusConfig--Topic--UserEvent";
        public const string BidLogTopicName = "ServiceBusConfig--Queue--BidLog";
    }

    /// <summary>
    /// Consolidates all well-known actor reminder names in a single convenient location.
    /// </summary>
    public struct ReminderNames
    {
        public const string HighBid = "high-bid";
        public const string ScheduledClose = "scheduled-close";
    }

    /// <summary>
    /// Consolidates all well-known service names in a single convenient location.
    /// </summary>
    public struct ServiceNames
    {
        public const string AuctionController = "BidsService.AuctionController";
        public const string LotActorService = "LotActorService";
        public const string AuctionActorService = "AuctionActorService";
    }

    /// <summary>
    /// Consolidates all well-known event names in a single convenient location.
    /// </summary>
    public struct EventNames
    {
        public const string LotClosedEvent = "LotClosedEvent";
        public const string LotCanceledEvent = "LotCanceledEvent";
        public const string LotUpdatedEvent = "LotUpdatedEvent";
        public const string HighBidEvent = "HighBidEvent";
        public const string AuctionClosedEvent = "AuctionClosedEvent";
        public const string ChoiceLotCreatedEvent = "ChoiceLotCreatedEvent";
    }

    /// <summary>
    /// Consolidates all logging context properties in a single convenient location.
    /// </summary>
    public struct LogContextProperties
    {
        public const string RequestId = "RequestId";
        public const string ItemId = "ItemId";
        public const string AuctionId = "AuctionId";
        public const string AuctionType = "AuctionType";
        public const string RingId = "RingId";
        public const string UserId = "UserId";
        public const string RequestData = "RequestData";
        public const string OperationName = "OperationName";
        public const string ChoiceLotGroupId = "ChoiceLotGroupId";
    }
}
