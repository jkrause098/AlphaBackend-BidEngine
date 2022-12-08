using System.Runtime.Serialization;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Implements a generic response from Web APIs.
/// </summary>
[DataContract]
public abstract class ApiResponse : ApiResponseBase<ApiResponseStatus>
{
}
