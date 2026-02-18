using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Interfaces;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriceParserController : ControllerBase
    {
        private readonly IPriceParserService _priceParserService;
        RecipePlannerContext _context;

        public PriceParserController(IPriceParserService priceParserService, RecipePlannerContext context)
        {
            _priceParserService = priceParserService;
            _context = context;
        }

        /// <summary>
        /// Получает цену продукта из магазина «Пятёрочка»
        /// </summary>
        /// <param name="productName">Название продукта для поиска (обязательно)</param>
        /// <param name="volume">Объём продукта (опционально, например: "2л", "500мл")</param>
        /// <returns>
        /// Цена в формате double (например, 166.99)
        /// -1: Товар не найден
        /// -2: Сайт недоступен
        /// -3: Ошибка парсинга / изменена структура страницы
        /// </returns>
        [HttpPost("parse")]
        public async Task<ActionResult<double>> ParsePrice(
            [FromBody] List<IngredientDto> ingredients,
            [FromQuery] string volume = null)
        {
            if (ingredients.Count == 0)
                return BadRequest("Нет продуктов");

            foreach (var ingredient in ingredients)
            {
                double price = await _priceParserService.ParsePriceAsync(ingredient.Name, volume);
                await _context.ShoppingListItems.FirstOrDefaultAsync(x => x.Name == ingredient.Name);
            }
            await _context.SaveChangesAsync();
            
            return Ok();
        }
    }
}