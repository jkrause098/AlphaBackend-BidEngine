using Newtonsoft.Json;

namespace AuctionPlatform.BidsService.Common.Serialization;

/// <summary>
/// Implements a component that provides custom JSON-based serialization with compression and decompression.
/// </summary>
public class JsonSerializer : ISerializer
{
    /// <summary>
    /// Returns the serialization settings.
    /// </summary>
    public JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    /// <summary>
    /// Serializes a typed object to a memory stream.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A memory stream containing the serialized object.</returns>
    public Stream Serialize<T>(T value)
    {
        var buffer = new MemoryStream();

        if (value != null)
        {
            using (var writer = new StreamWriter(buffer, leaveOpen: true))
            {
                writer.Write(JsonConvert.SerializeObject(value, value.GetType(), Formatting.None, SerializerSettings));
            }

            buffer.Seek(0, SeekOrigin.Begin);
        }

        return buffer;
    }

    /// <summary>
    /// Deserializes a memory stream to a strongly typed object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="stream">The memory stream containing the serialized object.</param>
    /// <returns>Returns an object of type <typeparamref name="T"/> deserialized from the specified stream.</returns>
    public T Deserialize<T>(Stream stream)
    {
        using var reader = new StreamReader(stream);

#pragma warning disable CS8603 // Possible null reference return.
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd(), SerializerSettings);
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Deserializes a memory stream to an untyped object. Implies that object type information is stored in the serialized object metadata.
    /// </summary>
    /// <param name="stream">The memory stream containing the serialized object.</param>
    /// <returns>Returns an object instance deserialized from the specified stream.</returns>
    public object Deserialize(Stream stream)
    {
        using var reader = new StreamReader(stream);

#pragma warning disable CS8603 // Possible null reference return.
        return JsonConvert.DeserializeObject(reader.ReadToEnd(), SerializerSettings);
#pragma warning restore CS8603 // Possible null reference return.
    }
}
