namespace AuctionPlatform.BidsService.Common;

/// <summary>
/// Provides a set of helper methods operating with various types of keys such as cache key, state keys, dictionary keys, etc.
/// </summary>
public static class KeyUtils
{
    public static string GetHighBidKey(string itemId)
    {
        return $"HB-{itemId.ToUpperInvariant()}";
    }

    public static string GetRingStateKey(string ringId)
    {
        return $"RING-STATE-{ringId.ToUpperInvariant()}";
    }

    public static string GetLotUpdatedEventKey(Guid itemId)
    {
        return $"LUE-STATE-{itemId.ToUpperCase()}";
    }

    public static string GetLotUpdatedEventKey(string itemId)
    {
        return $"LUE-STATE-{itemId.ToUpperInvariant()}";
    }

    public static Guid GetSequentialGuid()
    {
        var guidArray = Guid.NewGuid().ToByteArray();

        var baseDate = new DateTime(1900, 1, 1);
        var now = DateTime.Now;

        // Get the days and milliseconds which will be used to build the byte string 
        var days = new TimeSpan(now.Ticks - baseDate.Ticks);
        var msecs = now.TimeOfDay;

        // Convert to a byte array 
        // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
        var daysArray = BitConverter.GetBytes(days.Days);
        var msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

        // Reverse the bytes to match SQL Servers ordering 
        Array.Reverse(daysArray);
        Array.Reverse(msecsArray);

        // Copy the bytes into the guid 
        Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
        Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

        return new Guid(guidArray);
    }
}
