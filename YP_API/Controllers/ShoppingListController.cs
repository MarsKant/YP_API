using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Interfaces;
using YP_API.Models;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShoppingListController : ControllerBase
    {
        private readonly RecipePlannerContext _context;

        public ShoppingListController(RecipePlannerContext context)
        {
            _context = context;
        }

        [HttpPost("generate-from-menu/{menuId}/{userId}")]
        public async Task<ActionResult> GenerateFromMenu(int menuId, int userId)
        {
            try
            {
                var menu = await _context.Menus
                    .Include(m => m.Items)
                        .ThenInclude(i => i.Recipe)
                    .FirstOrDefaultAsync(m => m.Id == menuId && m.UserId == userId);

                if (menu == null)
                    return NotFound(new { error = "Меню не найдено" });

                var recipeIds = menu.Items.Select(i => i.RecipeId).ToList();

                var recipeIngredients = await _context.RecipeIngredients
                    .Include(ri => ri.Ingredient)
                    .Where(ri => recipeIds.Contains(ri.RecipeId))
                    .ToListAsync();

                var shoppingList = new ShoppingList
                {
                    UserId = userId,
                    Name = $"Список для {menu.Name}",
                    CreatedAt = DateTime.UtcNow
                };

                var ingredientGroups = recipeIngredients
                    .GroupBy(ri => ri.IngredientId)
                    .Select(g => new
                    {
                        Ingredient = g.First().Ingredient,
                        TotalQuantity = g.Sum(ri => ri.Quantity)
                    });

                foreach (var group in ingredientGroups)
                {
                    shoppingList.Items.Add(new ShoppingListItem
                    {
                        Name = group.Ingredient.Name,
                        Quantity = group.TotalQuantity,
                        Unit = group.Ingredient.Unit,
                        IsPurchased = false,
                        Price = 0
                    });
                }



                await _context.ShoppingLists.AddAsync(shoppingList);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Список покупок создан",
                    listId = shoppingList.Id,
                    itemCount = shoppingList.Items.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/current")]
        public async Task<ActionResult> GetCurrentShoppingList(int userId)
        {
            try
            {
                var list = await _context.ShoppingLists
                    .Include(sl => sl.Items)
                    .Where(sl => sl.UserId == userId && !sl.IsCompleted)
                    .OrderByDescending(sl => sl.CreatedAt)
                    .FirstOrDefaultAsync();

                if (list == null)
                    return Ok(new
                    {
                        success = true,
                        data = (object)null,
                        message = "Нет активных списков покупок"
                    });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Id = list.Id,
                        Name = list.Name,
                        IsCompleted = list.IsCompleted,
                        Items = list.Items.Select(i => new
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Quantity = i.Quantity,
                            Unit = i.Unit,
                            IsPurchased = i.IsPurchased
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("items/{itemId}/toggle")]
        public async Task<ActionResult> ToggleItemPurchased(int itemId)
        {
            try
            {
                var item = await _context.ShoppingListItems.FindAsync(itemId);
                if (item == null)
                    return NotFound(new { error = "Товар не найден" });

                item.IsPurchased = !item.IsPurchased;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = item.IsPurchased ? "Товар отмечен как купленный" : "Товар отмечен как некупленный"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}