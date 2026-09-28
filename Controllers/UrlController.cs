using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using YGS_URLTracker.DataModels;
using YGS_URLTracker.DataAccess;
using YGS_URLTracker.InternalServices;

namespace YGS_URLTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrlController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private IMemoryCache _cache;
        private const string LongToShortPrefix = "long:";
        private const string ShortToLongPrefix = "short:";

        public UrlController(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;

            //build cache

        }

        [HttpPost("/shorten")]
        public async Task<ActionResult<ShortenResponse>> Shorten(
        [FromBody] ShortenRequest request)
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
            {
                return BadRequest("A valid HTTP/HTTPS URL is required.");
            }

            DataAccessLayer DAL = new DataAccessLayer(_configuration);
            UrlRecord urlRecord = new UrlRecord();
            urlRecord.OriginalUrl = request.Url;
            int Count = 0;

            string LongUrlKey = LongToShortPrefix + request.Url;
            if (_cache.TryGetValue(LongUrlKey, out string? cachedShortUrl))
            {
                urlRecord.ShortenedUrl = cachedShortUrl;
                Count = await DAL.UpdateUrlTracking(urlRecord);

                ShortenResponse shortResponse = new ShortenResponse(urlRecord.ShortenedUrl);
                return Ok(shortResponse);
            }
            else //cache miss
            {
                //set cache, create short URL
                UrlShortener urlShortener = new UrlShortener();
                string ShortUrl = urlShortener.ShortenUrl(request.Url);

                string ShortUrlKey = ShortToLongPrefix + ShortUrl;

                _cache.Set(LongUrlKey, ShortUrl);
                _cache.Set(ShortUrlKey, request.Url);

                //update tracking
                await DAL.UpdateUrlTracking(urlRecord);     
                
                ShortenResponse shortenResponse = new ShortenResponse(ShortUrl);
                return Ok(shortenResponse);
            }
        }

        [HttpGet("/{shortUrl}")]
        public async Task<IActionResult> Redirect(string shortUrl)
        {
            //check cache
            string ShortUrlKey = ShortToLongPrefix + shortUrl;
            DataAccessLayer DAL = new DataAccessLayer(_configuration);
            int Count;

            if (_cache.TryGetValue(ShortUrlKey, out string? cachedLongUrl))
            {
                //UNCLEAR if a redirect updates analytics. Assuming "yes"
                UrlRecord urlRecord = new UrlRecord();
                urlRecord.OriginalUrl = cachedLongUrl;
                urlRecord.ShortenedUrl = shortUrl;
                
                Count = await DAL.UpdateUrlTracking(urlRecord);

                return await Redirect(cachedLongUrl);
            }
            else //cache miss, fetch from database
            {
                ShortenRequest request = new ShortenRequest(shortUrl);
                UrlRecord urlRecord = await DAL.GetUrlData(request);

                if (urlRecord != null)
                {
                    if (urlRecord.OriginalUrl.Length > 0)
                    {
                        _cache.Set(LongToShortPrefix + urlRecord.OriginalUrl, urlRecord.ShortenedUrl);
                        _cache.Set(ShortToLongPrefix + urlRecord.ShortenedUrl, urlRecord.OriginalUrl);

                        //Assume: update tracking
                        await DAL.UpdateUrlTracking(urlRecord);

                        return await Redirect(cachedLongUrl);

                    }
                    else //fail state; no cache and no entry in DB
                    {
                        //UNCLEAR what to do in this case. Going to return a "Not Found" for now
                        return NotFound();
                    }
                }
                else
                {
                    //UNCLEAR what to do in this case. Going to return a "Not Found" for now
                    return NotFound();
                }
            }
        }

        [HttpGet("/analytics/{shortUrl}")]
        public async Task<ActionResult<UrlRecord>> GetAnalytics(
            string shortUrl)
        {
            DataAccessLayer DAL = new DataAccessLayer(_configuration);
            ShortenRequest request = new ShortenRequest(shortUrl);
            UrlRecord urlRecord = new UrlRecord();
            urlRecord = await DAL.GetUrlData(request);

            if (urlRecord is null)
                return NotFound();

            return Ok(urlRecord);
        }

        private async void ConstructCache()
        {
            DataAccessLayer DAL = new DataAccessLayer(_configuration);

            _cache = await DAL.GetCacheData(LongToShortPrefix,ShortToLongPrefix);

        }
    }
}
