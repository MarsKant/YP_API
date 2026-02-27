using System.Text.Json;
using System.Text.RegularExpressions;

namespace YP_API.Services
{
    public class BuildIdCache
    {
        private record CacheEntry(string BuildId, DateTime ExpiresAt);

        private CacheEntry? _cache;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<BuildIdCache> _logger;
        private readonly TimeSpan _ttl = TimeSpan.FromHours(1);
        private readonly HttpClient _httpClient;

        public BuildIdCache(ILogger<BuildIdCache> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15),
                DefaultRequestHeaders =
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" }
            }
            };
        }

        public async Task<string?> GetOrFetchAsync()
        {
            if (_cache?.ExpiresAt > DateTime.UtcNow)
                return _cache.BuildId;

            await _lock.WaitAsync();
            try
            {
                if (_cache?.ExpiresAt > DateTime.UtcNow)
                    return _cache.BuildId;

                var buildId = await FetchBuildIdAsync();
                if (!string.IsNullOrEmpty(buildId))
                {
                    _cache = new CacheEntry(buildId, DateTime.UtcNow + _ttl);
                    _logger.LogDebug("buildId обновлён: {BuildId}", buildId);
                }
                return buildId;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<string?> FetchBuildIdAsync()
        {
            try
            {
                var html = await _httpClient.GetStringAsync("https://www.vprok.ru");

                var nextDataPattern = @"<script[^>]*id=""__NEXT_DATA__""[^>]*>(?<json>.*?)</script>";
                var match = Regex.Match(html, nextDataPattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    using var doc = JsonDocument.Parse(match.Groups["json"].Value);
                    var buildId = doc.RootElement.GetProperty("buildId").GetString();
                    if (!string.IsNullOrEmpty(buildId))
                        return buildId;
                }

                var staticPattern = @"/_next/static/([a-zA-Z0-9\-_]+)/_buildManifest\.js";
                var staticMatch = Regex.Match(html, staticPattern);
                if (staticMatch.Success)
                    return staticMatch.Groups[1].Value;

                _logger.LogWarning("Не удалось извлечь buildId из HTML");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении buildId");
                return null;
            }
        }

        public void Invalidate() => _cache = null;
    }
}
