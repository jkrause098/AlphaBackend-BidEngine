using Newtonsoft.Json;
using AuctionPlatform.BidsService.ApiContracts;

namespace AuctionPlatform.BidsService.Domain;

/// <summary>
/// Implements an object holding information about a bid made to a particular auction lot.
/// </summary>
public class BidLogEntry
{
    /// <summary>
    /// Gets or sets the unique ID of the bid.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string BidId { get; set; }

    /// <summary>
    /// Gets or sets the unique ID of the open auction.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the auction's lot being bid.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the location (US state) of the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string UserState { get; set; }

    /// <summary>
    /// Gets or sets the paddle number associated with the bidding user.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string UserPaddleNo { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? BidAmount { get; set; }

    /// <summary>
    /// Gets or sets the max bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? MaxBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the lowest acceptable bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? LowBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the highest acceptable bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? HighBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the next acceptable bid amount.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? NextBidAmount { get; set; }

    /// <summary>
    /// Gets or sets the item price after bid placement.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public decimal? ItemPrice { get; set; }

    /// <summary>
    /// Gets or sets the status of the bid.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public BidStatus BidStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this bid was placed.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public DateTimeOffset BidDateTime { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string RequestId { get; set; }
}
