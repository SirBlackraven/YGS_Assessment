using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Data.Common;
using YGS_URLTracker.DataModels;

namespace YGS_URLTracker.DataAccess
{
    public class DataAccessLayer
    {
        private string ConnString = "";

        private readonly IConfiguration _configuration;

        public DataAccessLayer(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urlRecord">struct-like class of inputs</param>
        /// <returns></returns>
        public async Task<int> UpdateUrlTracking(UrlRecord urlRecord)
        {
            await using var connection = new SqlConnection(ConnString);
            await using var command = new SqlCommand("dbo.yp_UpsertShortenedUrl", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@OriginalUrl", SqlDbType.VarChar, -1) { Value = urlRecord.OriginalUrl });
            command.Parameters.Add(new SqlParameter("@ShortUrl", SqlDbType.VarChar, -1) { Value = urlRecord.ShortenedUrl });

            // Output parameter
            var countParam = command.Parameters.Add(new SqlParameter("@Count", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            });

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int count = (int)countParam.Value;

            return count;
        }

        public async Task<UrlRecord> GetUrlData(ShortenRequest urlRecord)
        {
            await using var connection = new SqlConnection(ConnString);
            await using var command = new SqlCommand("dbo.GetUrlData_ByShortURL", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@ShortUrl", SqlDbType.VarChar, -1) { Value = urlRecord.Url });

            SqlDataReader reader = await command.ExecuteReaderAsync();

            UrlRecord urlData = new UrlRecord();
            while (reader.Read())
            {
                urlData.OriginalUrl = reader.GetString(reader.GetOrdinal("original_url"));
                urlData.ShortenedUrl = reader.GetString(reader.GetOrdinal("short_url"));
                urlData.CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
                urlData.ClickCount = reader.GetInt64(reader.GetOrdinal("click_count"));
            }

            return urlData;
        }

        public async Task<IMemoryCache> GetCacheData(string LongUrlKey, string ShortUrlKey)
        {
            string LongUrl = "";
            string ShortUrl = "";
            IMemoryCache memoryCache = null;

            await using var connection = new SqlConnection(ConnString);
            await using var command = new SqlCommand("dbo.GetAllUrlData", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            SqlDataReader reader = await command.ExecuteReaderAsync();

            while (reader.Read())
            {
                LongUrl = reader.GetString(reader.GetOrdinal("original_url"));
                ShortUrl = reader.GetString(reader.GetOrdinal("short_url"));

                memoryCache.Set(LongUrlKey + LongUrl, ShortUrl);
                memoryCache.Set(ShortUrlKey + ShortUrl, LongUrl);
            }

            return memoryCache;
        }
    }
}
