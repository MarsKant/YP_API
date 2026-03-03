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

        [HttpGet("fridge/{userId}")]
        public async Task<ActionResult> GetFridgeItemsByUserId(int userId)
        {
            try
            {
                var fridgeItems = await _context.FridgeItems
                    .Where(fi => fi.UserId == userId)
                    .Include(fi => fi.Ingredient)
                    .ToListAsync();

                if (fridgeItems.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        data = Array.Empty<object>()
                    });
                }

                var result = fridgeItems.Select(fi => new
                {
                    Id = fi.Id,
                    IngredientId = fi.IngredientId,
                    Name = fi.Ingredient?.Name,
                    Category = fi.Ingredient?.Category ?? "Неизвестно",
                    Unit = fi.Ingredient?.Unit,
                    Quantity = fi.Quantity
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("FridgeItem/add/{userId}")]
        public async Task<IActionResult> AddFridgeItem(int userId, int ingredientId, decimal? Quantity)
        {
            var existingIngredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Id == ingredientId);

            Ingredient ingredientToUse;

            
            if (existingIngredient == null)
            {
                return BadRequest();
            }

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
                    Quantity = Quantity,
                    Ingredient = existingIngredient
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
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
