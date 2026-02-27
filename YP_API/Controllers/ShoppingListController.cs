using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Models;

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
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.Id == menuId && m.UserId == userId);

            if (menu == null)
                return NotFound(new { error = "Меню не найдено" });

            var recipeIngredients = menu.Items
                .SelectMany(i => i.Recipe?.RecipeIngredients ?? Enumerable.Empty<RecipeIngredient>())
                .Where(ri => ri.Ingredient != null)
                .ToList();

            if (!recipeIngredients.Any())
                return BadRequest(new { error = "В рецептах меню нет ингредиентов" });

            var shoppingList = new ShoppingList
            {
                UserId = userId,
                Name = $"Список для {menu.Name}",
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            var ingredientGroups = recipeIngredients
                .GroupBy(ri => ri.IngredientId)
                .Select(g => new
                {
                    IngredientId = g.Key,
                    TotalQuantity = g.Sum(ri => ri.Quantity)
                });

            foreach (var group in ingredientGroups)
            {
                shoppingList.Items.Add(new ShoppingListItem
                {
                    IngredientId = group.IngredientId,
                    Quantity = group.TotalQuantity,
                    IsPurchased = false
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
                    .ThenInclude(i => i.Ingredient)  
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
                    CreatedAt = list.CreatedAt,
                    Items = list.Items.Select(i => new
                    {
                        Id = i.Id,
                        IngredientId = i.IngredientId,
                        Name = i.Ingredient?.Name,      
                        Unit = i.Ingredient?.Unit,      
                        Price = i.Ingredient?.Price,   
                        Quantity = i.Quantity,          
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
            var item = await _context.ShoppingListItems
                .Include(i => i.ShoppingList)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return NotFound(new { error = "Товар не найден" });

            item.IsPurchased = !item.IsPurchased;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = item.IsPurchased ? "Товар отмечен как купленный" : "Товар отмечен как некупленный",
                itemId = item.Id,
                isPurchased = item.IsPurchased
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("items/{itemId}/quantity")]
    public async Task<ActionResult> UpdateItemQuantity(int itemId, [FromBody] decimal quantity)
    {
        try
        {
            var item = await _context.ShoppingListItems.FindAsync(itemId);
            if (item == null)
                return NotFound(new { error = "Товар не найден" });

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, quantity = item.Quantity });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}