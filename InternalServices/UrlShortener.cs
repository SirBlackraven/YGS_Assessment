namespace YGS_URLTracker.InternalServices
{
    public class UrlShortener
    {
        public string ShortenUrl(string url)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            // Hash the URL into a positive number
            uint hash = (uint)url.GetHashCode();

            char[] code = new char[7];
            for (int i = 0; i < 7; i++)
            {
                code[i] = chars[(int)(hash % (uint)chars.Length)];
                hash /= (uint)chars.Length;

                // Handle remainder overflow with a simple multiplier shift
                if (hash == 0) hash = (uint)(url.Length * (i + 1) * 31);
            }

            return new string(code);
        }
    }
}
