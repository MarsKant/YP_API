using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Models;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly RecipePlannerContext _context;
        private readonly Interfaces.IMenuService _menuService;

        public InventoryController(RecipePlannerContext context, YP_API.Interfaces.IMenuService menuService)
        {
            _context = context;
            _menuService = menuService;
        }

        // 1. Получение списка продуктов (для мобилки)
        [HttpGet("fridge/{userId}")]
        public async Task<ActionResult> GetFridgeItemsByUserId(int userId)
        {
            try
            {
                var fridgeItems = await _context.FridgeItems
                    .Where(fi => fi.UserId == userId)
                    .Include(fi => fi.Ingredient)
                    .ToListAsync();

                var result = fridgeItems.Select(fi => new
                {
                    Id = fi.Id,
                    IngredientId = fi.IngredientId,
                    Name = fi.Ingredient?.Name,
                    Category = fi.Ingredient?.Category ?? "Неизвестно",
                    Unit = fi.Ingredient?.Unit,
                    Quantity = fi.Quantity
                }).ToList();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // 2. Добавление через форму (существующий метод)
        [HttpPost("FridgeItem/add/{userId}")]
        public async Task<IActionResult> AddFridgeItem(int userId, int ingredientId, decimal? Quantity)
        {
            var existingIngredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Id == ingredientId);

            if (existingIngredient == null) return BadRequest("Ингредиент не найден");

            var userInventory = await _context.FridgeItems
                .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IngredientId == existingIngredient.Id);

            if (userInventory != null)
            {
                userInventory.Quantity += Quantity ?? 1;
            }
            else
            {
                _context.FridgeItems.Add(new FridgeItem
                {
                    UserId = userId,
                    IngredientId = existingIngredient.Id,
                    Quantity = Quantity ?? 1,
                    Ingredient = existingIngredient
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // 3. ИСПРАВЛЕННЫЙ МЕТОД: Прием данных из мобильного приложения
        [HttpPost("add/{userId}")]
        public async Task<IActionResult> AddToInventory(int userId, [FromBody] InventoryAddRequest request)
        {
            try
            {
                // Ищем ингредиент по имени, которое прислала мобилка
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Name.ToLower() == request.ProductName.ToLower());

                if (ingredient == null)
                {
                    // Если такого ингредиента нет в базе, можно либо создать его, 
                    // либо вернуть ошибку. Пока вернем ошибку для надежности:
                    return BadRequest(new { success = false, message = "Ингредиент не найден в базе" });
                }

                var existingItem = await _context.FridgeItems
                    .FirstOrDefaultAsync(fi => fi.UserId == userId && fi.IngredientId == ingredient.Id);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    // Используем FridgeItem вместо несуществующего InventoryItem
                    var newItem = new FridgeItem
                    {
                        UserId = userId,
                        IngredientId = ingredient.Id,
                        Quantity = request.Quantity
                    };
                    _context.FridgeItems.Add(newItem);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Добавлено в холодильник" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("user/{userId}/ingredient/{ingredientId}")]
        public async Task<IActionResult> RemoveFromFridge(int userId, int ingredientId)
        {
            var item = await _context.FridgeItems
                .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IngredientId == ingredientId);

            if (item == null) return NotFound();

            _context.FridgeItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public class InventoryAddRequest
        {
            public string ProductName { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
        }
    }
}