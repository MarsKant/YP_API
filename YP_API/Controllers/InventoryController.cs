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
        private readonly YP_API.Interfaces.IMenuService _menuService;

        public InventoryController(RecipePlannerContext context, YP_API.Interfaces.IMenuService menuService)
        {
            _context = context;
            _menuService = menuService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetFridge(int userId)
        {
            var inventory = await _context.FridgeItems
                .Include(ui => ui.Ingredient)
                .Where(ui => ui.UserId == userId)
                .Select(ui => new
                {
                    Id = ui.IngredientId,
                    Name = ui.Ingredient.Name,
                    Quantity = ui.Quantity,
                    Unit = ui.Unit,
                    Category = ui.Ingredient.Category
                })
                .ToListAsync();

            return Ok(new { success = true, data = inventory });
        }

        [HttpPost("FridgeItem/add/{userId}")]
        public async Task<IActionResult> AddFridgeItem(int userId, [FromBody] Ingredient ingredientDto)
        {
            // 1. Ищем, есть ли уже такой ингредиент в общем справочнике (без учета регистра)
            var existingIngredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Name.ToLower() == ingredientDto.Name.ToLower());

            Ingredient ingredientToUse;

            if (existingIngredient == null)
            {
                // 2. Если нет — создаем новый
                ingredientToUse = new Ingredient
                {
                    Name = ingredientDto.Name,
                    Category = ingredientDto.Category ?? "Разное",
                    Unit = ingredientDto.Unit ?? "шт"
                    
                };
                _context.Ingredients.Add(ingredientToUse);
                await _context.SaveChangesAsync(); // Сохраняем, чтобы получить Id
            }
            else
            {
                // 3. Если есть — берем существующий
                ingredientToUse = existingIngredient;
            }

            // 4. Теперь проверяем, есть ли этот продукт уже в холодильнике у пользователя
            var userInventory = await _context.FridgeItems
                .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IngredientId == ingredientToUse.Id);

            if (userInventory != null)
            {
                // Если есть — просто увеличиваем количество
                userInventory.Quantity += 1;
            }
            else
            {
                // Если нет — добавляем новую запись в холодильник
                _context.FridgeItems.Add(new FridgeItem
                {
                    UserId = userId,
                    ProductName = ingredientToUse.Name,
                    IngredientId = ingredientToUse.Id,
                    Quantity = 1,
                    Unit = ingredientToUse.Unit
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("get/{userId}")]
        public async Task<IActionResult> GetFridgeItemsByUserId(int userId)
        {
            try
            {
                List<FridgeItem> fridgeItems = await _context.FridgeItems.Where(x => x.UserId == userId).ToListAsync();
                return Ok(fridgeItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
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

        public class InventoryItemDto
        {
            public int IngredientId { get; set; }
            public decimal Quantity { get; set; }
            public string? Unit { get; set; }
        }
    }
}
