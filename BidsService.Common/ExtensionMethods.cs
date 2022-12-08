namespace AuctionPlatform.BidsService.Common
{
    /// <summary>
    /// Provides value-add extension methods for various system .NET objects.
    /// </summary>
    public static class ExtensionMethods
    {
        public static string ToUpperCase(this Guid value)
        {
            return value.ToString().ToUpper();
        }

        public static string RemoveAsyncSuffix(this string value)
        {
            return !string.IsNullOrEmpty(value) ? value.Replace("Async", string.Empty) : value;
        }
    }
}
