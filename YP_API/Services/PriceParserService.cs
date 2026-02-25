using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Text.Json;
using YP_API.Controllers;
using YP_API.Data;
using YP_API.Interfaces;

namespace YP_API.Services
{
    public class PriceParserService : IPriceParserService
    {
        private readonly IBrowser _browser;
        private readonly ILogger<PriceParserService> _logger;
        private readonly RecipePlannerContext _context;

        public PriceParserService(IBrowser browser, ILogger<PriceParserService> logger, RecipePlannerContext context)
        {
            _browser = browser;
            _logger = logger;
            _context = context;
        }

        public async Task<bool> ParsePriceAsync(List<IngredientDto> ingredients)
        {
            if (!ingredients.Any()) return false;

            int updatedCount = 0;
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                Locale = "ru-RU"
            });

            var page = await context.NewPageAsync();
            var random = new Random();

            try
            {
                foreach (var ingredient in ingredients)
                {
                    string searchUrl = $"https://www.vprok.ru/catalog/search?text={Uri.EscapeDataString(ingredient.Name)}";

                    await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                    await page.WaitForTimeoutAsync(random.Next(2000, 4000));

                    var jsonData = await page.EvaluateAsync<string>(@"() => {
                        const script = document.querySelector('#__NEXT_DATA__');
                        return script ? script.textContent : null;
                        }");

                    if (string.IsNullOrEmpty(jsonData))
                    {
                        _logger.LogWarning($"__NEXT_DATA__ не найден для: {ingredient.Name}");
                        continue;
                    }

                    using var jsonDoc = JsonDocument.Parse(jsonData);
                    var root = jsonDoc.RootElement;

                    var products = ExtractProductsFromJson(root, ingredient.Name);

                    if (products.Any())
                    {
                        var dbItem = await _context.Ingredients
                            .FirstOrDefaultAsync(x => x.Name == ingredient.Name);

                        if (dbItem != null)
                        {
                            dbItem.Price = products.First().Price;
                            _logger.LogInformation($"Обновлено: {ingredient.Name} = {products.First().Price} руб.");
                            updatedCount++;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Товар не найден: {ingredient.Name}");
                    }

                    await page.WaitForTimeoutAsync(random.Next(1000, 2000));
                }

                if (updatedCount > 0) await _context.SaveChangesAsync();
                return updatedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка парсинга: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }
        }

        private List<ProductInfo> ExtractProductsFromJson(JsonElement root, string ingredientName)
        {
            var products = new List<ProductInfo>();

            try
            {
                if (!root.TryGetProperty("props", out var props)) return products;
                if (!props.TryGetProperty("pageProps", out var pageProps)) return products;
                if (!pageProps.TryGetProperty("initialStore", out var initialStore)) return products;
                if (!initialStore.TryGetProperty("searchPage", out var searchPage)) return products;
                if (!searchPage.TryGetProperty("products", out var productsArray)) return products;

                foreach (var product in productsArray.EnumerateArray())
                {
                    if (!product.TryGetProperty("name", out var nameElement)) continue;
                    string productName = nameElement.GetString() ?? "";

                    if (!IsNameMatch(productName, ingredientName)) continue;

                    decimal price = 0;
                    if (product.TryGetProperty("price", out var priceElement))
                    {
                        if (priceElement.ValueKind == JsonValueKind.Number)
                            price = (decimal)priceElement.GetDouble();
                        else if (priceElement.ValueKind == JsonValueKind.String)
                            decimal.TryParse(priceElement.GetString(), out price);
                    }

                    if (price > 0)
                    {
                        products.Add(new ProductInfo { Name = productName, Price = price });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка парсинга JSON: {ex.Message}");
            }

            return products;
        }

        private bool IsNameMatch(string productName, string ingredientName)
        {
            var productWords = productName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ingredientWords = ingredientName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return ingredientWords.All(w => productWords.Any(pw => pw.Contains(w)));
        }

        private class ProductInfo
        {
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
        }
    }
}