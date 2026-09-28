namespace YGS_URLTracker.DataModels
{
    public sealed record ShortenRequest(
    string Url);

    public sealed record ShortenResponse(
        string ShortUrl);

    public class UrlRecord
    {
        public string OriginalUrl { get; set; } = "";
        public string ShortenedUrl { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public long ClickCount { get; set; }

        public UrlRecord()
        {
        }

        public UrlRecord(string originalUrl, string shortenedUrl, DateTime? createdAt, long clickCount)
        {
            OriginalUrl = originalUrl;
            ShortenedUrl = shortenedUrl;
            CreatedAt = createdAt;
            ClickCount = clickCount;
        }
    }
}
