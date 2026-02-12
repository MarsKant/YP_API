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

            // Используем качественный User-Agent и настройки контекста
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                DeviceScaleFactor = 1
            });

            var page = await context.NewPageAsync();

            try
            {
                string searchUrl = $"https://5ka.ru/search/?text={Uri.EscapeDataString(productName + (volume != null ? " " + volume : ""))}";
                _logger.LogInformation($"[Playwright] Переход по URL: {searchUrl}");

                // Переходим на страницу с имитацией поведения человека
                await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });

                // Небольшая задержка, чтобы контент успел отрендериться (защита от ботов любит быстрые переходы)
                await page.WaitForTimeoutAsync(1500);

                // Закрываем модалку города, если она есть
                try
                {
                    var cityBtn = page.GetByRole(AriaRole.Button, new() { Name = "Да" });
                    if (await cityBtn.IsVisibleAsync()) await cityBtn.ClickAsync();
                }
                catch { }

                var cardLocator = page.Locator("article, a[href*='/product/'], [class*='product-card']");

                try
                {
                    await cardLocator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
                }
                catch (Exception)
                {
                    _logger.LogWarning($"[Playwright] Товары не найдены на странице для: {productName}");
                    return -1;
                }

                var cards = await cardLocator.AllAsync();

                foreach (var card in cards)
                {
                    try
                    {
                        var text = await card.InnerTextAsync();
                        if (string.IsNullOrWhiteSpace(text)) continue;

                        if (!text.Contains(productName, StringComparison.OrdinalIgnoreCase)) continue;

                        if (!string.IsNullOrEmpty(volume))
                        {
                            if (!IsVolumeMatch(text, volume))
                            {
                                continue;
                            }
                        }

                        // Логика извлечения цены
                        string cleanText = text.Replace("?", "").Replace("₽", "").Replace("\n", " ").Trim();
                        var matches = Regex.Matches(cleanText, @"(\d{1,})\s*(\d{2})"); // Поиск целых и копеек

                        if (matches.Count > 0)
                        {
                            var lastMatch = matches.Cast<Match>().Last();
                            string priceStr = $"{lastMatch.Groups[1].Value}.{lastMatch.Groups[2].Value}";

                            if (double.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double price))
                            {
                                _logger.LogInformation($"[MATCH] Найдено: {productName} - {price} руб.");
                                return price;
                            }
                        }
                    }
                    catch { continue; }
                }

                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Playwright] Ошибка парсинга: {ex.Message}");
                return -1;
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }
        }

        private bool IsVolumeMatch(string text, string requestedVolume)
        {
            var reqMatch = Regex.Match(requestedVolume.ToLower(), @"(\d+[,.]?\d*)\s*(мл|л|г|кг|ml|l|g|kg)");
            if (!reqMatch.Success) return text.Contains(requestedVolume, StringComparison.OrdinalIgnoreCase);

            double reqValue = double.Parse(reqMatch.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            string reqUnit = reqMatch.Groups[2].Value;

            if (reqUnit == "л" || reqUnit == "l" || reqUnit == "кг" || reqUnit == "kg") reqValue *= 1000;

            var textMatches = Regex.Matches(text.ToLower(), @"(\d+[,.]?\d*)\s*(мл|л|г|кг|ml|l|g|kg)");
            foreach (Match m in textMatches)
            {
                double val = double.Parse(m.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                string unit = m.Groups[2].Value;

                if (unit == "л" || unit == "l" || unit == "кг" || unit == "kg") val *= 1000;
                if (Math.Abs(val - reqValue) < (reqValue * 0.01)) return true;
            }

            return false;
        }
    }
}