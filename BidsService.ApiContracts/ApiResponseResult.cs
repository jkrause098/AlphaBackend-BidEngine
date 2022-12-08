using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Defines a base class for all responses from Web APIs return a result.
/// </summary>
[DataContract]
public class ApiResponseResult<TResult, TStatus> : ApiResponseBase<TStatus>
{
    /// <summary>
    /// The overall status of the request.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.Result, Required = Required.AllowNull)]
    [DataMember]
    public TResult Result { get; set; }
}
