namespace AuctionPlatform.BidsService.Common.Serialization;

/// <summary>
/// Defines a contract that must be supported by a custom serializer.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes a typed object to a memory stream.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A memory stream containing the serialized object.</returns>
    Stream Serialize<T>(T value);

    /// <summary>
    /// Deserializes a memory stream to a strongly typed object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="stream">The memory stream containing the serialized object.</param>
    /// <returns>Returns an object of type <typeparamref name="T"/> deserialized from the specified stream.</returns>
    T Deserialize<T>(Stream stream);

    /// <summary>
    /// Deserializes a memory stream to an untyped object. Implies that object type information is stored in the serialized object metadata.
    /// </summary>
    /// <param name="stream">The memory stream containing the serialized object.</param>
    /// <returns>Returns an object instance deserialized from the specified stream.</returns>
    object Deserialize(Stream stream);
}
