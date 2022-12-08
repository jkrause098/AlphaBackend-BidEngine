using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Serilog;
using Serilog.Context;
using SerilogTimings.Extensions;
using AuctionPlatform.BidsService.ApiContracts;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Domain;
using AuctionPlatform.BidsService.Infrastructure;
using AuctionPlatform.BidsService.Interfaces;
using SullivanAuctioneers.Common.Data;
using AuctionPlatform.Contract.Interfaces;
using AuctionPlatform.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;
using BidStatus = AuctionPlatform.BidsService.ApiContracts.BidStatus;

namespace AuctionPlatform.BidsService.WebApi.Controllers;

/// <summary>
/// Implements the API controller for auctions.
/// </summary>
[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly ILogger logger;
    private readonly IDbContext dbContext;
    private readonly IValidator<OpenAuctionRequest> openRequestValidator;
    private readonly IValidator<CloseAuctionRequest> closeRequestValidator;
    private readonly IValidator<Auction> auctionValidator;
    private readonly IValidator<AuctionInfo> auctionInfoValidator;
    private readonly IValidator<PlaceBidRequest> placeBidRequestValidator;
    private readonly IValidator<RetrieveBidsRequest> retrieveLotBidsRequestValidator;
    private readonly IValidator<DeleteBidsRequest> deleteLotBidsRequestValidator;
    private readonly IValidator<RetrieveHighBidsRequest> retrieveHighBidsRequestValidator;
    private readonly IValidator<SellLotRequest> sellLotRequestValidator;
    private readonly IValidator<CancelLotRequest> cancelLotRequestValidator;
    private readonly IValidator<UpdateLotRequest> updateLotRequestValidator;
    private readonly IValidator<GetAuctionInfoRequest> getAuctionInfoRequestValidator;
    private readonly IValidator<OpenChoiceLotRequest> openChoiceLotRequestValidator;
    private readonly IValidator<SellChoiceLotRequest> sellChoiceLotRequestValidator;
    private readonly ITransformer<BidIncrementTableDetail, BidIncrement> bidIncrementTransform;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionController"/> class.
    /// </summary>
    /// <param name="logger">The logging object.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="openRequestValidator">The validation component.</param>
    /// <param name="closeRequestValidator">The validation component.</param>
    /// <param name="auctionValidator">The validation component.</param>
    /// <param name="auctionInfoValidator">The validation component.</param>
    /// <param name="placeBidRequestValidator">The validation component.</param>
    /// <param name="retrieveLotBidsRequestValidator">The validation component.</param>
    /// <param name="deleteLotBidsRequestValidator">The validation component.</param>
    /// <param name="retrieveHighBidsRequestValidator">The validation component.</param>
    /// <param name="sellLotRequestValidator">The validation component.</param>
    /// <param name="cancelLotRequestValidator">The validation component.</param>
    /// <param name="updateLotRequestValidator">The validation component.</param>
    /// <param name="getAuctionInfoRequestValidator">The validation component.</param>
    /// <param name="openChoiceLotRequestValidator">The validation component.</param>
    /// <param name="sellChoiceLotRequestValidator">The validation component.</param>
    /// <param name="bidIncrementTransform">The data transformation component.</param>
    public AuctionController(ILogger logger,
                             IDbContext dbContext,
                             IValidator<OpenAuctionRequest> openRequestValidator,
                             IValidator<CloseAuctionRequest> closeRequestValidator,
                             IValidator<Auction> auctionValidator,
                             IValidator<AuctionInfo> auctionInfoValidator,
                             IValidator<PlaceBidRequest> placeBidRequestValidator,
                             IValidator<RetrieveBidsRequest> retrieveLotBidsRequestValidator,
                             IValidator<DeleteBidsRequest> deleteLotBidsRequestValidator,
                             IValidator<RetrieveHighBidsRequest> retrieveHighBidsRequestValidator,
                             IValidator<SellLotRequest> sellLotRequestValidator,
                             IValidator<CancelLotRequest> cancelLotRequestValidator,
                             IValidator<UpdateLotRequest> updateLotRequestValidator,
                             IValidator<GetAuctionInfoRequest> getAuctionInfoRequestValidator,
                             IValidator<OpenChoiceLotRequest> openChoiceLotRequestValidator,
                             IValidator<SellChoiceLotRequest> sellChoiceLotRequestValidator,
                             ITransformer<BidIncrementTableDetail, BidIncrement> bidIncrementTransform)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.openRequestValidator = openRequestValidator;
        this.closeRequestValidator = closeRequestValidator;
        this.auctionValidator = auctionValidator;
        this.auctionInfoValidator = auctionInfoValidator;
        this.placeBidRequestValidator = placeBidRequestValidator;
        this.retrieveLotBidsRequestValidator = retrieveLotBidsRequestValidator;
        this.deleteLotBidsRequestValidator = deleteLotBidsRequestValidator;
        this.retrieveHighBidsRequestValidator = retrieveHighBidsRequestValidator;
        this.sellLotRequestValidator = sellLotRequestValidator;
        this.cancelLotRequestValidator = cancelLotRequestValidator;
        this.updateLotRequestValidator = updateLotRequestValidator;
        this.getAuctionInfoRequestValidator = getAuctionInfoRequestValidator;
        this.openChoiceLotRequestValidator = openChoiceLotRequestValidator;
        this.sellChoiceLotRequestValidator = sellChoiceLotRequestValidator;
        this.bidIncrementTransform = bidIncrementTransform;
    }

    /// <summary>
    /// Opens the specified auction to allow placing bids
    /// </summary>
    /// <param name="auctionId">The ID of the auction.</param>
    /// <returns>The response object.</returns>
    [HttpPost]
    [Route("open/{auctionId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(OpenAuctionResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(OpenAuctionResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(OpenAuctionResponse))]
    public async Task<IActionResult> OpenAuction(string auctionId)
    {
        // Package input parameters into a handy request object.
        var request = new OpenAuctionRequest
        {
            AuctionId = auctionId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = openRequestValidator.Validate(request);

        // Reject the bad data.
        if (!validationResult.IsValid) return BadRequest(new OpenAuctionResponse
        {
            AuctionId = request.AuctionId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.OpenAuctionInvalidRequest,
            Status = AuctionStatus.NotOpened,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenAuction)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestData, request, true))
        using (logger.TimeOperation("Opening auction {AuctionId}", request.AuctionId))
        {
            try
            {
                // Make sure the specified auction actually exists.
                var auction = await dbContext.Auctions.Where(a => a.Id == Guid.Parse(request.AuctionId)).FirstOrDefaultAsync();

                if (auction == null) return NotFound(new OpenAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId,
                    Message = $"Auction ID {request.AuctionId} cannot be found",
                    MessageCode = MessageCode.Frontend.OpenAuctionInvalidAuctionId,
                    Status = AuctionStatus.NotFound
                });

                validationResult = auctionValidator.Validate(auction);
                if (!validationResult.IsValid) return BadRequest(new OpenAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId,
                    Message = "Auction data is invalid",
                    MessageCode = MessageCode.Frontend.OpenAuctionInvalidData,
                    Status = AuctionStatus.NotOpened,
                    Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
                });

                // Load bid increments that is configured for this auction.
                var bidIncrements = dbContext.BidIncrementTableDetails.Where(a => a.BidIncrementTableId == auction.BidIncrementTableId);

                // Load all items that are linked to each seller of this auction.
                var auctionLots = from item in dbContext.Items
                                  join seller in dbContext.AuctionSellers
                                  on item.AuctionSellerId equals seller.Id
                                  where seller.AuctionId == auction.Id && (!item.ScheduledCloseDate.HasValue || item.ScheduledCloseDate >= DateTime.UtcNow)
                                  select new AuctionLot
                                  {
                                      AuctionId = auction.Id,
                                      AuctionType = auction.AuctionType,
                                      ItemId = item.Id,
                                      ItemLotNo = item.LotNo,
                                      SellerId = seller.Id,
                                      ScheduledCloseDate = item.ScheduledCloseDate.ToUtc(),
                                      Status = (LotStatus)item.ItemBidStatusId
                                  };

                var auctionInfo = new AuctionInfo
                {
                    AuctionId = auction.Id,
                    AuctionNo = auction.AuctionNo,
                    AuctionType = auction.AuctionType,
                    FirstItemScheduledCloseUtc = auction.FirstItemScheduledCloseUtc,
                    BidIncrements = await bidIncrements.Select(i => bidIncrementTransform.Transform(i)).ToArrayAsync(),
                    Lots = await auctionLots.ToArrayAsync()
                };

                // Validate auction information so that we don't start this auction with potentially bad inputs.
                validationResult = auctionInfoValidator.Validate(auctionInfo);
                if (!validationResult.IsValid) return BadRequest(new OpenAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId,
                    Message = "Auction information is invalid",
                    MessageCode = MessageCode.Frontend.OpenAuctionInvalidData,
                    Status = AuctionStatus.NotOpened,
                    Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
                });

                // Create a remoting proxy for the specified auction.
                var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(Guid.Parse(request.AuctionId)));

                // Invoke the actor's operation.
                var result = await auctionActor.OpenAuctionAsync(request, auctionInfo);

                if (result.Success)
                {
                    return new OkObjectResult(result);
                }

                return new BadRequestObjectResult(result);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while opening auction {AuctionId}", request.AuctionId);

                return StatusCode((int)HttpStatusCode.InternalServerError, new OpenAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Frontend.OpenAuctionServerError,
                    Status = AuctionStatus.NotOpened
                });
            }
        }
    }

    /// <summary>
    /// Retrieves the details about the specified auction
    /// </summary>
    /// <param name="auctionId">The ID of the auction.</param>
    /// <returns>The response object.</returns>
    [HttpGet]
    [Route("{auctionId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(GetAuctionInfoResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(GetAuctionInfoResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(GetAuctionInfoResponse))]
    public async Task<IActionResult> GetAuctionInfo(string auctionId)
    {
        // Package input parameters into a handy request object.
        var request = new GetAuctionInfoRequest
        {
            AuctionId = auctionId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = getAuctionInfoRequestValidator.Validate(request);

        // Reject the bad data.
        if (!validationResult.IsValid) return BadRequest(new GetAuctionInfoResponse
        {
            AuctionId = request.AuctionId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.GetAuctionInfoInvalidRequest,
            Status = AuctionStatus.NotOpened,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(GetAuctionInfo)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (logger.TimeOperation("Retrieving details for auction {AuctionId}", request.AuctionId))
        {
            try
            {
                // Create a remoting proxy for the specified auction.
                var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(Guid.Parse(request.AuctionId)));

                // Invoke the actor's operation.
                var result = await auctionActor.GetAuctionInfoAsync(request);

                if (result.Success)
                {
                    return new OkObjectResult(result);
                }

                return new BadRequestObjectResult(result);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while retrieving auction details");

                return StatusCode((int)HttpStatusCode.InternalServerError, new GetAuctionInfoResponse
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Frontend.GetAuctionInfoServerError,
                    Status = AuctionStatus.NotOpened
                });
            }
        }
    }

    /// <summary>
    /// Stops and closes the specified auction along with all of its lots
    /// </summary>
    /// <param name="auctionId">The ID of the auction.</param>
    /// <returns>The response object.</returns>
    [HttpDelete]
    [Route("{auctionId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(CloseAuctionResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(CloseAuctionResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(CloseAuctionResponse))]
    public async Task<IActionResult> CloseAuction(string auctionId)
    {
        // Package input parameters into a handy request object.
        var request = new CloseAuctionRequest
        {
            AuctionId = auctionId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = closeRequestValidator.Validate(request);

        // Reject the bad data.
        if (!validationResult.IsValid) return BadRequest(new CloseAuctionResponse
        {
            AuctionId = request.AuctionId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.CloseAuctionInvalidRequest,
            Status = AuctionStatus.NotOpened,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(CloseAuction)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (logger.TimeOperation("Closing auction {AuctionId}", request.AuctionId))
        {
            try
            {
                // Create a remoting proxy for the specified auction.
                var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(Guid.Parse(request.AuctionId)));

                // Invoke the actor's operation.
                var result = await auctionActor.CloseAuctionAsync(request);

                if (result.Success)
                {
                    return new OkObjectResult(result);
                }

                return new BadRequestObjectResult(result);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while closing auction");

                return StatusCode((int)HttpStatusCode.InternalServerError, new CloseAuctionResponse
                {
                    AuctionId = request.AuctionId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Frontend.CloseAuctionServerError,
                    Status = AuctionStatus.NotOpened
                });
            }
        }
    }

    /// <summary>
    /// Places a bid on the open auction
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <returns>The response object.</returns>
    [HttpPost]
    [Route("bid")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(PlaceBidResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(PlaceBidResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(PlaceBidResponse))]
    public async Task<IActionResult> PlaceBid([FromBody] PlaceBidRequest request)
    {
        // Protect against bad data.
        var validationResult = placeBidRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new PlaceBidResponse
        {
            AuctionId = request.AuctionId,
            ItemId = request.ItemId,
            RequestId = Request.HttpContext.TraceIdentifier,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.PlaceBidInvalidRequest,
            Status = BidStatus.Invalid,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(PlaceBid)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.UserId, request.UserId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestData, request, true))
        using (logger.TimeOperation("Placing bid for item {ItemId}", request.ItemId))
        {
            // Request ID is not expected to be supplied by the client, it must be assigned here.
            request.RequestId = Request.HttpContext.TraceIdentifier;

            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.ItemId)));

            // Create a remoting service proxy object used for communication with Auction Controller.
            var auctionController = AppServiceProxy.Create<IAuctionController>(GlobalConsts.ServiceNames.AuctionController, request.AuctionId);

            // Invoke the actor's operation.
            var result = await lotActor.PlaceBidAsync(request);

            // Track the current bid unless it was accepted (accepted bids are being communicated by the actor itself).
            if (result.Status != BidStatus.Accepted)
            {
                // This action takes care of sending rejected and other non-winning bids to a Service Bus queue.
                await auctionController.TrackBidAsync(request, result);
            }

            // Log the details about the bid, this will aid troubleshooting.
            using (LogContext.PushProperty(nameof(PlaceBidResponse.LowBidAmount), result.LowBidAmount))
            using (LogContext.PushProperty(nameof(PlaceBidResponse.HighBidAmount), result.HighBidAmount))
            using (LogContext.PushProperty(nameof(PlaceBidResponse.NextBidAmount), result.NextBidAmount))
            using (LogContext.PushProperty(nameof(PlaceBidResponse.ItemPrice), result.ItemPrice))
            {
                logger.Information("Bid amount {BidAmount} has been {BidStatus}", result.BidAmount, result.Status.ToString());
            }

            // Handle the result and return the appropriate response back to the client.
            if (result.Success)
            {
                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Retrieves all current bids for the specified item in the open auction
    /// </summary>
    /// <param name="itemId">The ID of the auction lot (item).</param>
    /// <returns>The response object.</returns>
    [HttpGet]
    [Route("bids/lot/{itemId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(RetrieveBidsResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(RetrieveBidsResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(RetrieveBidsResponse))]
    public async Task<IActionResult> RetrieveLotBids([FromRoute] string itemId)
    {
        var request = new RetrieveBidsRequest
        {
            ItemId = itemId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = retrieveLotBidsRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new RetrieveBidsResponse
        {
            ItemId = request.ItemId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.RetrieveLotBidsInvalidRequest,
            Status = ApiResponseStatus.Failure,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(RetrieveLotBids)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (logger.TimeOperation("Retrieving bids for item {ItemId}", request.ItemId))
        {
            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.ItemId)));

            // Invoke the actor's operation.
            var result = await lotActor.RetrieveBidsAsync(request);

            if (result.Success)
            {
                if (result.Status == ApiResponseStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Retrieves all current high bids for the specified collection of items in the open auction
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <returns>The response object.</returns>
    [HttpPost]
    [Route("bids/high")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(RetrieveHighBidsResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(RetrieveHighBidsResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(RetrieveHighBidsResponse))]
    public async Task<IActionResult> RetrieveHighBids([FromBody] RetrieveHighBidsRequest request)
    {
        // Protect against bad data.
        var validationResult = retrieveHighBidsRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new RetrieveHighBidsResponse
        {
            AuctionId = request.AuctionId,
            RequestId = Request.HttpContext.TraceIdentifier,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.RetrieveHighBidsInvalidRequest,
            Status = ApiResponseStatus.Failure,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(RetrieveHighBids)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestData, request, true))
        using (logger.TimeOperation("Retrieving high bids for {ItemCount} item(s)", request.ItemIds.Length))
        {
            // Request ID is not expected to be supplied by the client, it must be assigned here.
            request.RequestId = Request.HttpContext.TraceIdentifier;

            // Create a remoting service proxy object used for communication with Auction Controller.
            var auctionController = AppServiceProxy.Create<IAuctionController>(GlobalConsts.ServiceNames.AuctionController, request.AuctionId);

            // Invoke the remoting service operation.
            var result = await auctionController.RetrieveHighBidsAsync(request);

            if (result.Success)
            {
                if (result.Status == ApiResponseStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Deletes the last bid placed for the specified item in the open auction
    /// </summary>
    /// <param name="itemId">The ID of the auction lot (item).</param>
    /// <returns>The response object.</returns>
    [HttpDelete]
    [Route("bids/lot/{itemId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(DeleteBidsResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(DeleteBidsResponse))]
    [SwaggerResponse(404, "No bids were found", typeof(DeleteBidsResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(DeleteBidsResponse))]
    public async Task<IActionResult> DeleteLotBids([FromRoute] string itemId)
    {
        var request = new DeleteBidsRequest
        {
            ItemId = itemId,
            RequestId = Request.HttpContext.TraceIdentifier,
            LastOnly = true
        };

        // Protect against bad data.
        var validationResult = deleteLotBidsRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new DeleteBidsResponse
        {
            ItemId = request.ItemId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.DeleteLotBidsInvalidRequest,
            Status = ApiResponseStatus.Failure,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(DeleteLotBids)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (logger.TimeOperation("Deleting bids for item {ItemId}", request.ItemId))
        {
            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.ItemId)));

            // Invoke the actor's operation.
            var result = await lotActor.DeleteBidsAsync(request);

            if (result.Success)
            {
                if (result.Status == ApiResponseStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Sells the specified item in the open auction to the user who placed highest bid
    /// </summary>
    /// <param name="itemId">The ID of the auction lot (item) to be sold.</param>
    /// <returns>The response object.</returns>
    [HttpPut]
    [Route("sale/lot/{itemId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(SellLotResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(SellLotResponse))]
    [SwaggerResponse(404, "No bids were found", typeof(SellLotResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(SellLotResponse))]
    public async Task<IActionResult> SellLot([FromRoute] string itemId)
    {
        var request = new SellLotRequest
        {
            ItemId = itemId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = sellLotRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new SellLotResponse
        {
            ItemId = request.ItemId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.SellLotInvalidRequest,
            Status = SaleStatus.Invalid,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(SellLot)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (logger.TimeOperation("Selling item {ItemId}", request.ItemId))
        {
            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.ItemId)));

            // Invoke the actor's operation.
            var result = await lotActor.SellLotAsync(request);

            if (result.Success)
            {
                if (result.Status == SaleStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Cancels the specified item and removes it from the auction
    /// </summary>
    /// <param name="itemId">The ID of the auction lot (item) to be canceled.</param>
    /// <returns>The response object.</returns>
    [HttpDelete]
    [Route("sale/lot/{itemId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(CancelLotResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(CancelLotResponse))]
    [SwaggerResponse(404, "No bids were found", typeof(CancelLotResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(CancelLotResponse))]
    public async Task<IActionResult> CancelLot([FromRoute] string itemId)
    {
        var request = new CancelLotRequest
        {
            ItemId = itemId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = cancelLotRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new CancelLotResponse
        {
            ItemId = request.ItemId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.CancelLotInvalidRequest,
            Status = SaleStatus.Invalid,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(CancelLot)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (logger.TimeOperation("Canceling item {ItemId}", request.ItemId))
        {
            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.ItemId)));

            // Invoke the actor's operation.
            var result = await lotActor.CancelLotAsync(request);

            if (result.Success)
            {
                if (result.Status == SaleStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Updates the real-time status of the specified lot
    /// </summary>
    /// <param name="itemId">The ID of the auction lot (item) to be updated.</param>
    /// <param name="request">The request object.</param>
    /// <returns>The response object.</returns>
    [HttpPut]
    [Route("lot/{itemId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(UpdateLotResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(UpdateLotResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(UpdateLotResponse))]
    public async Task<IActionResult> UpdateLot([FromRoute] string itemId, [FromBody] UpdateLotRequest request)
    {
        // Add data to the request object that was not supposed to be supplied by the caller.
        request.ItemId = itemId;
        request.RequestId = Request.HttpContext.TraceIdentifier;

        // Protect against bad data.
        var validationResult = updateLotRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new UpdateLotResponse
        {
            ItemId = request.ItemId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.UpdateLotInvalidRequest,
            Status = ApiResponseStatus.Failure,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(UpdateLot)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ItemId, request.ItemId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestData, request, true))
        using (logger.TimeOperation("Updating item {ItemId}", request.ItemId))
        {
            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.ItemId)));

            // Invoke the actor's operation.
            var result = await lotActor.UpdateLotAsync(request);

            if (result.Success)
            {
                if (result.Status == ApiResponseStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Creates a choice lot from the group of lots stored in the database under the specified <paramref name="groupId"/>
    /// </summary>
    /// <param name="auctionId">The ID of the auction that owns the choice lot.</param>
    /// <param name="groupId">The ID of the choice lot group which must exist in the database.</param>
    /// <returns>The response object.</returns>
    [HttpPost]
    [Route("{auctionId}/choice/{groupId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(OpenChoiceLotResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(OpenChoiceLotResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(OpenChoiceLotResponse))]
    public async Task<IActionResult> OpenChoiceLot([FromRoute] string auctionId, [FromRoute] string groupId)
    {
        var request = new OpenChoiceLotRequest
        {
            AuctionId = auctionId,
            GroupId = groupId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = openChoiceLotRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new OpenChoiceLotResponse
        {
            AuctionId = request.AuctionId,
            GroupId = request.GroupId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.OpenChoiceLotInvalidRequest,
            Status = ApiResponseStatus.Failure,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenChoiceLot)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.AuctionId, request.AuctionId))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ChoiceLotGroupId, request.GroupId))
        using (logger.TimeOperation("Opening choice lot group {ChoiceLotGroupId}", request.GroupId))
        {
            // Find out what choice lot group actually consists of.
            var groupItems = await dbContext.ChoiceGroupItems.Where(i => i.ChoiceGroupId == Guid.Parse(request.GroupId) && i.IsSelected).Select(g => g.ItemId).ToListAsync();

            // Make sure the specified choice lot group actually contains any items.
            if (groupItems.Count == 0) return NotFound(new OpenChoiceLotResponse
            {
                AuctionId = request.AuctionId,
                GroupId = request.GroupId,
                RequestId = request.RequestId,
                Message = $"Choice lot group {request.GroupId} cannot be found or it doesn't have any lots selected",
                MessageCode = MessageCode.Frontend.OpenChoiceLotGroupNotFound,
                Status = ApiResponseStatus.NotFound
            });

            // Load all lots that are linked this choice lot group.
            var auctionLots = from item in dbContext.Items
                              join seller in dbContext.AuctionSellers on item.AuctionSellerId equals seller.Id
                              join auction in dbContext.Auctions on seller.AuctionId equals auction.Id
                              where seller.AuctionId == Guid.Parse(request.AuctionId) && groupItems.Any(i => i == item.Id)
                              select new AuctionLot
                              {
                                  AuctionId = auction.Id,
                                  AuctionType = auction.AuctionType,
                                  ItemId = item.Id,
                                  ItemLotNo = item.LotNo,
                                  SellerId = seller.Id,
                                  ScheduledCloseDate = item.ScheduledCloseDate.ToUtc(),
                                  Status = (LotStatus)item.ItemBidStatusId
                              };

            // Create a remoting proxy for the specified auction.
            var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(Guid.Parse(request.AuctionId)));

            // Invoke the actor's operation.
            var result = await auctionActor.OpenChoiceLotAsync(request, await auctionLots.ToArrayAsync());

            if (result.Success)
            {
                if (result.Status == ApiResponseStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                return new OkObjectResult(result);
            }

            return new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    /// Sells the specified choice lot in the open auction to the user who placed highest bid
    /// </summary>
    /// <param name="groupId">The ID of the choice lot to be sold.</param>
    /// <param name="request">The request object.</param>
    /// <returns>The response object.</returns>
    [HttpPut]
    [Route("sale/choice/{groupId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(SellChoiceLotResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(SellLotResponse))]
    [SwaggerResponse(404, "No bids were found", typeof(SellLotResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(SellLotResponse))]
    public async Task<IActionResult> SellChoiceLot([FromRoute] string groupId, [FromBody] SellChoiceLotRequest request)
    {
        // Add data to the request object that was not supposed to be supplied by the caller.
        request.GroupId = groupId;
        request.RequestId = Request.HttpContext.TraceIdentifier;

        // Protect against bad data.
        var validationResult = sellChoiceLotRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new SellLotResponse
        {
            ItemId = request.GroupId,
            RequestId = request.RequestId,
            UserId = request.UserId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.SellChoiceLotInvalidRequest,
            Status = SaleStatus.Invalid,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(SellChoiceLot)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.ChoiceLotGroupId, request.GroupId))
        using (logger.TimeOperation("Selling choice lot {ChoiceLotGroupId}", request.GroupId))
        {
            // Create a remoting proxy for the specified auction lot (item).
            var lotActor = ActorProxy.Create<ILotActor>(new ActorId(Guid.Parse(request.GroupId)));

            // Create a request to retrieve the lot's current bids.
            var getBidsRequest = new RetrieveBidsRequest
            {
                ItemId = request.GroupId,
                RequestId = request.RequestId
            };

            // Retrieve current bids for this choice lot.
            var getBidsResponse = await lotActor.RetrieveBidsAsync(getBidsRequest);

            // Handle response to validate highest bidder.
            if (getBidsResponse.Success && getBidsResponse.Bids != null && getBidsResponse.Bids.Length > 0)
            {
                // Figure out the highest bid.
                var highBid = getBidsResponse.Bids.OrderByDescending(i => i.BidAmount).First();

                // Make sure the user specified in the request is in fact our highest bidder.
                if (highBid.UserId != request.UserId)
                {
                    // Reject this request if user doesn't match.
                    return BadRequest(new SellLotResponse
                    {
                        ItemId = request.GroupId,
                        RequestId = request.RequestId,
                        UserId = request.UserId,
                        Message = "The specified user is not highest bidder on this choice lot",
                        MessageCode = MessageCode.Frontend.SellChoiceLotUserNotHighBidder,
                        Status = SaleStatus.Invalid
                    });
                }
            }
            else
            {
                return BadRequest(new SellLotResponse
                {
                    ItemId = request.GroupId,
                    RequestId = request.RequestId,
                    UserId = request.UserId,
                    Message = getBidsResponse.Message,
                    MessageCode = getBidsResponse.MessageCode,
                    Status = SaleStatus.Invalid,
                    Errors = getBidsResponse.Errors
                });
            }

            // Create a request object to mark the choice lot as sold.
            var sellRequest = new SellLotRequest
            {
                ItemId = request.GroupId,
                RequestId = request.RequestId
            };

            // Invoke the actor's operation.
            var result = await lotActor.SellLotAsync(sellRequest);

            // Depending on the outcome of the last operation, we have additional work to do.
            if (result.Success)
            {
                if (result.Status == SaleStatus.NotFound)
                {
                    return new NotFoundObjectResult(result);
                }

                // Create a remoting proxy for the specified auction.
                var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(Guid.Parse(result.AuctionId)));

                // Invoke the actor's operation.
                var saleResult = await auctionActor.SellChoiceLotAsync(request);

                if (saleResult.Success)
                {
                    if (saleResult.Status == SaleStatus.NotFound)
                    {
                        return new NotFoundObjectResult(saleResult);
                    }

                    return new OkObjectResult(saleResult);
                }

                return new BadRequestObjectResult(saleResult);
            }

            return new BadRequestObjectResult(result);
        }
    }
}
