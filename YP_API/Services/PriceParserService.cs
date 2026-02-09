using Microsoft.Playwright;
using System.Text.RegularExpressions;
using YP_API.Interfaces;

namespace YP_API.Services
{
    public class PriceParserService : IPriceParserService
    {
        private readonly IBrowser _browser;
        private readonly ILogger<PriceParserService> _logger;

        public PriceParserService(IBrowser browser, ILogger<PriceParserService> logger)
        {
            _browser = browser;
            _logger = logger;
        }

        public async Task<double> ParsePriceAsync(string productName, string? volume = null)
        {
            if (string.IsNullOrWhiteSpace(productName)) return -1;

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 1000 }
            });

            var page = await context.NewPageAsync();

            try
            {
                // Если объем указан, добавляем его в поисковый запрос для точности самого сайта
                string searchUrl = $"https://5ka.ru/search/?text={Uri.EscapeDataString(productName + (volume != null ? " " + volume : ""))}";
                _logger.LogInformation($"[Playwright] Поиск: {productName} {volume}");

                await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Commit });

                try { await page.GetByRole(AriaRole.Button, new() { Name = "Да" }).ClickAsync(new() { Timeout = 3000 }); } catch { }

                var cardLocator = page.Locator("article, a[href*='/product/'], [class*='product-card']");
                await cardLocator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

                var cards = await cardLocator.AllAsync();

                foreach (var card in cards)
                {
                    try
                    {
                        var text = await card.InnerTextAsync(new() { Timeout = 2000 });
                        if (string.IsNullOrWhiteSpace(text)) continue;

                        // Проверка соответствия имени
                        if (!text.Contains(productName, StringComparison.OrdinalIgnoreCase)) continue;

                        // КРИТИЧНО: Проверка объема
                        if (!string.IsNullOrEmpty(volume))
                        {
                            if (!IsVolumeMatch(text, volume))
                            {
                                _logger.LogInformation($"[SKIP] Не подошел объем: {text.Replace("\n", " ")}");
                                continue;
                            }
                        }

                        _logger.LogInformation($"[MATCH] Найдено: {text.Replace("\n", " ")}");

                        // Поиск цены (берем последнее число)
                        string cleanText = text.Replace("?", "").Replace("₽", "").Trim();
                        var matches = Regex.Matches(cleanText, @"(\d{2,})\s*(\d{2})?");

                        if (matches.Count > 0)
                        {
                            var lastMatch = matches.Cast<Match>().Last();
                            string priceStr = lastMatch.Groups[2].Success
                                ? $"{lastMatch.Groups[1].Value}.{lastMatch.Groups[2].Value}"
                                : lastMatch.Value.Length > 2 ? lastMatch.Value.Insert(lastMatch.Value.Length - 2, ".") : lastMatch.Value;

                            if (double.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double price))
                            {
                                if (price < 10 && matches.Count > 1) continue;
                                return price;
                            }
                        }
                    }
                    catch { continue; }
                }

                return -1;
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }
        }

        // Умное сравнение объемов (930мл == 0.93л)
        private bool IsVolumeMatch(string text, string requestedVolume)
        {
            // Извлекаем число и единицу измерения из запроса (например, "930" и "мл")
            var reqMatch = Regex.Match(requestedVolume.ToLower(), @"(\d+[,.]?\d*)\s*(мл|л|г|кг|ml|l|g|kg)");
            if (!reqMatch.Success) return text.Contains(requestedVolume, StringComparison.OrdinalIgnoreCase);

            double reqValue = double.Parse(reqMatch.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            string reqUnit = reqMatch.Groups[2].Value;

            // Приводим всё к базовым единицам (мл или г)
            if (reqUnit == "л" || reqUnit == "l" || reqUnit == "кг" || reqUnit == "kg") reqValue *= 1000;

            // Ищем все упоминания объема в тексте карточки
            var textMatches = Regex.Matches(text.ToLower(), @"(\d+[,.]?\d*)\s*(мл|л|г|кг|ml|l|g|kg)");
            foreach (Match m in textMatches)
            {
                double val = double.Parse(m.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                string unit = m.Groups[2].Value;

                if (unit == "л" || unit == "l" || unit == "кг" || unit == "kg") val *= 1000;

                // Если разница меньше 1% (погрешность округления), значит это наш объем
                if (Math.Abs(val - reqValue) < (reqValue * 0.01)) return true;
            }

            return false;
        }
    }
}