using Microsoft.AspNetCore.Mvc;
using YP_API.Interfaces;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriceParserController : ControllerBase
    {
        private readonly IPriceParserService _priceParserService;

        public PriceParserController(IPriceParserService priceParserService)
        {
            _priceParserService = priceParserService;
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
        [HttpGet("parse")]
        public async Task<ActionResult<double>> ParsePrice(
            [FromQuery] string productName,
            [FromQuery] string volume = null)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return BadRequest("Название продукта обязательно для заполнения");

            double price = await _priceParserService.ParsePriceAsync(productName, volume);
            return Ok(price);
        }
    }
}