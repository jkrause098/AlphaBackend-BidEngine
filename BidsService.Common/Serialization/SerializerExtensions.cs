using System.Text;

namespace AuctionPlatform.BidsService.Common.Serialization;

/// <summary>
/// Provides value-add extension methods for <see cref="ISerializer"/> objects.
/// </summary>
public static class SerializerExtensions
{
    /// <summary>
    /// Deserializes a string containing the data to a typed object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="serializer">The <see cref="ISerializer"/> object which this method extends.</param>
    /// <param name="data">The object data in a string representation.</param>
    /// <returns>Returns an object of type <typeparamref name="T"/> deserialized from the specified <paramref name="data"/>.</returns>
    public static T Deserialize<T>(this ISerializer serializer, string data)
    {
        using var buffer = new MemoryStream(Encoding.UTF8.GetBytes(data));
        return serializer.Deserialize<T>(buffer);
    }

    /// <summary>
    /// Deserializes a byte array containing the data to a typed object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="serializer">The <see cref="ISerializer"/> object which this method extends.</param>
    /// <param name="data">The object data in a string representation.</param>
    /// <returns>Returns an object of type <typeparamref name="T"/> deserialized from the specified <paramref name="data"/>.</returns>
    public static T Deserialize<T>(this ISerializer serializer, byte[] data)
    {
        using var buffer = new MemoryStream(data);
        return serializer.Deserialize<T>(buffer);
    }

    /// <summary>
    /// Consumes all content from the input stream into a byte array.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <returns>The content of the input stream.</returns>
    public static async Task<byte[]> ReadAllBytesAsync(this Stream input)
    {
        var stream = input as MemoryStream;
        if (stream != null) return stream.ToArray();

        using var memoryStream = new MemoryStream();
        await input.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}
