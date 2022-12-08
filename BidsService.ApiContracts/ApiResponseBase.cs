using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AuctionPlatform.BidsService.ApiContracts;

/// <summary>
/// Defines a base class for all responses from Web APIs.
/// </summary>
[DataContract]
public abstract class ApiResponseBase<TStatus>
{
    /// <summary>
    /// The outcome of the request.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.Success, Required = Required.Always)]
    [DataMember]
    public bool Success { get; set; }

    /// <summary>
    /// The overall status of the request.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.Status, Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    [DataMember]
    public TStatus Status { get; set; }

    /// <summary>
    /// The message localized in client's language that must be displayed on the client.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.Message, NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string Message { get; set; }

    /// <summary>
    /// The message code that can be used by the consumer to map the message returned by the service into
    /// its localized version or a different message depending on the consumer's needs.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.MessageCode, NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string MessageCode { get; set; }

    /// <summary>
    /// The unique ID of the request assigned by the server when handling the client's request.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.RequestId, Required = Required.Always)]
    [DataMember]
    public string RequestId { get; set; }

    /// <summary>
    /// The list of errors that are associated with a failed operation.
    /// </summary>
    [JsonProperty(ApiConstants.JsonPropertyNames.Errors, NullValueHandling = NullValueHandling.Ignore)]
    [DataMember]
    public string[] Errors { get; set; }
}
