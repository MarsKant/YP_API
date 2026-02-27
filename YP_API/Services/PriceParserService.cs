using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using YP_API.Controllers;
using YP_API.Data;
using YP_API.Interfaces;
using YP_API.Services;

public class PriceParserService : IPriceParserService
{
    private readonly IBrowser _browser;
    private readonly ILogger<PriceParserService> _logger;
    private readonly RecipePlannerContext _context;

    public PriceParserService(
        IBrowser browser,
        ILogger<PriceParserService> logger,
        RecipePlannerContext context)
    {
        _browser = browser;
        _logger = logger;
        _context = context;
    }

    public async Task<bool> ParsePriceAsync(List<IngredientDto> ingredients)
    {
        if (ingredients == null || !ingredients.Any()) return false;

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        await context.RouteAsync("**/*.{png,jpg,jpeg,webp,svg,gif,woff,woff2}", route => route.AbortAsync());

        await context.AddInitScriptAsync(@"() => {
        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
        window.chrome = { runtime: {} };
    }");

        var page = await context.NewPageAsync();
        int updatedCount = 0;
        var random = new Random();

        try
        {
            await page.GotoAsync("https://www.vprok.ru", new() { WaitUntil = WaitUntilState.Load, Timeout = 30000 });

            var buildId = await page.EvaluateAsync<string>("() => window.__NEXT_DATA__.buildId");

            foreach (var ingredient in ingredients)
            {
                try
                {
                    var searchUrl = $"https://www.vprok.ru/catalog/search?text={Uri.EscapeDataString(ingredient.Name)}";

                    await page.GotoAsync(searchUrl, new()
                    {
                        WaitUntil = WaitUntilState.Load,
                        Timeout = 30000
                    });

                    try
                    {
                        await page.WaitForSelectorAsync("[class*='ProductCard'], [class*='EmptyState']",
                            new() { Timeout = 10000 });
                    }
                    catch {}

                    var jsonData = await page.EvaluateAsync<string>("() => JSON.stringify(window.__NEXT_DATA__ || {})");

                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        using var doc = JsonDocument.Parse(jsonData);
                        var root = doc.RootElement;
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
                    }
                    await page.WaitForTimeoutAsync(random.Next(1000, 2000));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка при поиске '{ingredient.Name}': {ex.Message}");
                    if (ex.Message.Contains("503"))
                    {
                        _logger.LogCritical("Обнаружена блокировка 503. Прерываем цикл.");
                        break;
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
        return updatedCount > 0;
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