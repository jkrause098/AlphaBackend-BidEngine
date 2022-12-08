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
using AuctionPlatform.BidsService.Interfaces;
using SullivanAuctioneers.Common.Data;
using AuctionPlatform.Contract.Interfaces;
using AuctionPlatform.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace AuctionPlatform.BidsService.WebApi.Controllers;

/// <summary>
/// Implements the API controller for auction rings.
/// </summary>
[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("[controller]")]
public class RingController : ControllerBase
{
    private readonly ILogger logger;
    private readonly IDbContext dbContext;
    private readonly IValidator<OpenRingRequest> openRequestValidator;
    private readonly IValidator<Ring> ringValidator;
    private readonly IValidator<AuctionInfo> auctionInfoValidator;
    private readonly IValidator<UpdateRingRequest> updateRingRequestValidator;
    private readonly ITransformer<BidIncrementTableDetail, BidIncrement> bidIncrementTransform;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionController"/> class.
    /// </summary>
    /// <param name="logger">The logging object.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="openRequestValidator">The validation component.</param>
    /// <param name="ringValidator">The validation component.</param>
    /// <param name="auctionInfoValidator">The validation component.</param>
    /// <param name="updateRingRequestValidator">The validation component.</param>
    /// <param name="bidIncrementTransform">The data transformation component.</param>
    public RingController(ILogger logger,
                          IDbContext dbContext,
                          IValidator<OpenRingRequest> openRequestValidator,
                          IValidator<Ring> ringValidator,
                          IValidator<AuctionInfo> auctionInfoValidator,
                          IValidator<UpdateRingRequest> updateRingRequestValidator,
                          ITransformer<BidIncrementTableDetail, BidIncrement> bidIncrementTransform)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.openRequestValidator = openRequestValidator;
        this.ringValidator = ringValidator;
        this.auctionInfoValidator = auctionInfoValidator;
        this.updateRingRequestValidator = updateRingRequestValidator;
        this.bidIncrementTransform = bidIncrementTransform;
    }

    /// <summary>
    /// Opens the specified ring to allow placing bids
    /// </summary>
    /// <param name="ringId">The ID of the ring.</param>
    /// <returns>The response object.</returns>
    [HttpPost]
    [Route("open/{ringId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(OpenRingResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(OpenRingResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(OpenRingResponse))]
    public async Task<IActionResult> OpenRing([FromRoute] string ringId)
    {
        // Package input parameters into a handy request object.
        var request = new OpenRingRequest
        {
            RingId = ringId,
            RequestId = Request.HttpContext.TraceIdentifier
        };

        // Protect against bad data.
        var validationResult = openRequestValidator.Validate(request);

        // Reject the bad data.
        if (!validationResult.IsValid) return BadRequest(new OpenRingResponse
        {
            RingId = request.RingId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.OpenRingInvalidRequest,
            Status = AuctionStatus.NotOpened,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(OpenRing)))
        using (logger.TimeOperation("Opening ring {RingId}", request.RingId))
        {
            try
            {
                // Request ID is not expected to be supplied by the client, it must be assigned here.
                request.RequestId = Request.HttpContext.TraceIdentifier;

                // Make sure the specified ring actually exists.
                var ring = await dbContext.Rings.Where(a => a.Id == Guid.Parse(request.RingId)).Include(a => a.Auction).FirstOrDefaultAsync();

                if (ring == null) return NotFound(new OpenRingResponse
                {
                    RingId = request.RingId,
                    RequestId = request.RequestId,
                    Message = $"Ring ID {request.RingId} cannot be found",
                    MessageCode = MessageCode.Frontend.OpenRingNotFound,
                    Status = AuctionStatus.NotFound
                });

                validationResult = ringValidator.Validate(ring);
                if (!validationResult.IsValid) return BadRequest(new OpenRingResponse
                {
                    RingId = request.RingId,
                    RequestId = request.RequestId,
                    Message = "Ring data is invalid",
                    MessageCode = MessageCode.Frontend.OpenRingInvalidData,
                    Status = AuctionStatus.NotOpened,
                    Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
                });

                // Load bid increments that is configured for this auction.
                var bidIncrements = dbContext.BidIncrementTableDetails.Where(a => a.BidIncrementTableId == ring.Auction.BidIncrementTableId);

                // Load all items that are linked to this ring.
                var auctionLots = from item in dbContext.Items
                                  where item.RingId == ring.Id
                                  select new AuctionLot
                                  {
                                      AuctionId = ring.AuctionId,
                                      AuctionType = ring.Auction.AuctionType,
                                      ItemId = item.Id,
                                      SellerId = item.AuctionSellerId,
                                      ScheduledCloseDate = item.ScheduledCloseDate.ToUtc(),
                                      Status = (LotStatus)item.ItemBidStatusId
                                  };

                // Combine all details into a single object for convenience.
                var auctionInfo = new AuctionInfo
                {
                    AuctionId = ring.Auction.Id,
                    AuctionNo = ring.Auction.AuctionNo,
                    AuctionType = ring.Auction.AuctionType,
                    RingId = ring.Id,
                    FirstItemScheduledCloseUtc = ring.FirstItemScheduledCloseUtc,
                    BidIncrements = await bidIncrements.Select(i => bidIncrementTransform.Transform(i)).ToArrayAsync(),
                    Lots = await auctionLots.ToArrayAsync()
                };

                // Validate auction information so that we don't start this auction with potentially bad inputs.
                validationResult = auctionInfoValidator.Validate(auctionInfo);
                if (!validationResult.IsValid) return BadRequest(new OpenRingResponse
                {
                    AuctionId = ring.AuctionId.ToUpperCase(),
                    RingId = ring.Id.ToUpperCase(),
                    RequestId = request.RequestId,
                    Message = "Auction ring information is invalid",
                    MessageCode = MessageCode.Frontend.OpenRingInvalidData,
                    Status = AuctionStatus.NotOpened,
                    Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
                });

                // Create a remoting proxy for the specified auction.
                var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(auctionInfo.AuctionId));

                // Invoke the actor's operation.
                var result = await auctionActor.OpenRingAsync(request, auctionInfo);

                if (result.Success)
                {
                    return new OkObjectResult(result);
                }

                return new BadRequestObjectResult(result);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while opening ring {RingId}", request.RingId);

                return StatusCode((int)HttpStatusCode.InternalServerError, new OpenRingResponse
                {
                    RingId = request.RingId,
                    RequestId = request.RequestId,
                    Message = ex.Message,
                    MessageCode = MessageCode.Frontend.OpenRingServerError,
                    Status = AuctionStatus.NotOpened
                });
            }
        }
    }

    /// <summary>
    /// Updates the details of the specified ring
    /// </summary>
    /// <param name="ringId">The ID of the auction ring to be updated.</param>
    /// <param name="request">The request object.</param>
    /// <returns>The response object.</returns>
    [HttpPut]
    [Route("{ringId}")]
    [SwaggerResponse(200, "Request has been accepted, check Status field to determine the outcome", typeof(UpdateRingResponse))]
    [SwaggerResponse(400, "Request data is invalid", typeof(UpdateRingResponse))]
    [SwaggerResponse(500, "Unexpected error occurred", typeof(UpdateRingResponse))]
    public async Task<IActionResult> UpdateRing([FromRoute] string ringId, [FromBody] UpdateRingRequest request)
    {
        // Add data to the request object that was not supposed to be supplied by the caller.
        request.RingId = ringId;
        request.RequestId = Request.HttpContext.TraceIdentifier;

        // Protect against bad data.
        var validationResult = updateRingRequestValidator.Validate(request);
        if (!validationResult.IsValid) return BadRequest(new UpdateRingResponse
        {
            RingId = request.RingId,
            RequestId = request.RequestId,
            Message = "Request data is invalid",
            MessageCode = MessageCode.Frontend.UpdateRingInvalidRequest,
            Status = ApiResponseStatus.Failure,
            Errors = validationResult.Errors.Select(i => i.ErrorMessage).ToArray()
        });

        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.OperationName, nameof(UpdateRing)))
        using (LogContext.PushProperty(GlobalConsts.LogContextProperties.RequestData, request, true))
        using (logger.TimeOperation("Updating ring {ItemId}", request.RingId))
        {
            // Make sure the specified ring actually exists.
            var ring = await dbContext.Rings.Where(a => a.Id == Guid.Parse(request.RingId)).FirstOrDefaultAsync();

            if (ring == null) return NotFound(new UpdateRingResponse
            {
                RingId = request.RingId,
                RequestId = request.RequestId,
                Message = $"Ring ID {request.RingId} cannot be found",
                MessageCode = MessageCode.Frontend.UpdateRingNotFound,
                Status = ApiResponseStatus.NotFound
            });

            // Load all items that are linked to this ring.
            var auctionLots = from item in dbContext.Items
                              where item.RingId == ring.Id
                              select new AuctionLot
                              {
                                  AuctionId = ring.AuctionId,
                                  ItemId = item.Id,
                                  ScheduledCloseDate = item.ScheduledCloseDate.ToUtc()
                              };

            // Combine all details into a single object for convenience.
            var auctionInfo = new AuctionInfo
            {
                AuctionId = ring.AuctionId,
                RingId = ring.Id,
                Lots = await auctionLots.ToArrayAsync()
            };

            // Create a remoting proxy for the specified auction.
            var auctionActor = ActorProxy.Create<IAuctionActor>(new ActorId(ring.AuctionId));

            // Invoke the actor's operation.
            var result = await auctionActor.UpdateRingAsync(request, auctionInfo);

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
}
