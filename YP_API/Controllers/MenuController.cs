using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Models;

namespace YP_API.Controllers
{
    public class MenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RecipeCount { get; set; }
        public IEnumerable<DateTime> Dates { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly RecipePlannerContext _context;

        public MenuController(RecipePlannerContext context)
        {
            _context = context;
        }

        //[HttpGet("available")]
        //public async Task<ActionResult> GetAvailableMenus()
        //{
        //    try
        //    {
        //        var menus = await _context.Menus
        //            .Include(m => m.Items)
        //                .ThenInclude(i => i.Recipe)
        //            .Select(m => new
        //            {
        //                Id = m.Id,
        //                Name = m.Name,
        //                CreatedAt = m.CreatedAt,
        //                TotalDays = m.Items.Select(i => i.Date.Date).Distinct().Count(),
        //                Recipes = m.Items.Select(i => new
        //                {
        //                    Id = i.RecipeId,
        //                    Title = i.Recipe.Title,
        //                    Date = i.Date.ToString("yyyy-MM-dd"),
        //                    MealType = i.MealType
        //                })
        //            })
        //            .ToListAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            data = menus
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { error = ex.Message });
        //    }
        //}

        [HttpPost("{menuId}/select/{userId}")]
        public async Task<ActionResult> SelectMenu(int menuId, int userId)
        {
            try
            {
                var menu = await _context.Menus
                    .Include(m => m.Items)
                    .FirstOrDefaultAsync(m => m.Id == menuId);

                if (menu == null)
                    return NotFound(new { error = "Меню не найдено" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { error = "Пользователь не найден" });

                var userMenu = new Menu
                {
                    UserId = userId,
                    Name = $"{menu.Name} (Выбрано)",
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var item in menu.Items)
                {
                    userMenu.Items.Add(new MenuItem
                    {
                        RecipeId = item.RecipeId,
                        Date = item.Date,
                        MealType = item.MealType
                    });
                }

                await _context.Menus.AddAsync(userMenu);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Меню успешно выбрано",
                    menuId = userMenu.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("generate-week/{userId}")]
        public async Task<ActionResult> GenerateWeekMenu(int userId)
        {
            try
            {
                var inventoryIngredientIds = await _context.FridgeItems
                    .Where(ui => ui.UserId == userId)
                    .Select(ui => ui.IngredientId)
                    .ToListAsync();

                Console.WriteLine($"[GenerateWeekMenu] User {userId} inventory ingredients: {string.Join(", ", inventoryIngredientIds)}");


                var candidateRecipes = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .Where(r => 
                       
                        r.RecipeIngredients.All(ri => inventoryIngredientIds.Contains(ri.IngredientId))
                        && r.RecipeIngredients.Count > 0)
                    .ToListAsync();

                Console.WriteLine($"[GenerateWeekMenu] Found {candidateRecipes.Count} candidate recipes with STRICT ingredient matching");
                foreach (var recipe in candidateRecipes)
                {
                    var recipeIngredients = recipe.RecipeIngredients.Select(ri => $"({ri.IngredientId})").ToList();
                    Console.WriteLine($"  - Recipe {recipe.Id}: {recipe.Title} - ВСЕ ингредиенты в наличии: {string.Join(", ", recipeIngredients)}");
                }

                if (!candidateRecipes.Any())
                {
                    Console.WriteLine($"[GenerateWeekMenu] Нет рецептов с ВСЕ необходимыми ингредиентами. Детали:");
                    
                    var allRecipes = await _context.Recipes
                        .Include(r => r.RecipeIngredients)
                        .ToListAsync();
                    
                    foreach (var recipe in allRecipes.Take(5))
                    {
                        var missingIngredients = recipe.RecipeIngredients
                            .Where(ri => !inventoryIngredientIds.Contains(ri.IngredientId))
                            .Select(ri => ri.IngredientId)
                            .ToList();
                        
                        if (missingIngredients.Any())
                        {
                            Console.WriteLine($"  - {recipe.Title}: не хватает ингредиентов {string.Join(", ", missingIngredients)}");
                        }
                    }
                    
                    return BadRequest(new { error = "Нет рецептов со ВСЕМИ необходимыми ингредиентами в холодильнике" });
                }

                var userMenu = new Menu
                {
                    UserId = userId,
                    Name = "Автоматическое меню на неделю",
                    CreatedAt = DateTime.UtcNow
                };

                var today = DateTime.Today;
                var mealTypes = new[] { "Завтрак", "Обед", "Ужин" };
                var rnd = new Random();
                for (int day = 0; day < 7; day++)
                {
                    var date = today.AddDays(day);
                    foreach (var mealType in mealTypes)
                    {
                        var recipe = candidateRecipes[rnd.Next(candidateRecipes.Count)];
                        userMenu.Items.Add(new MenuItem
                        {
                            RecipeId = recipe.Id,
                            Date = date,
                            MealType = mealType
                        });
                    }
                }

                await _context.Menus.AddAsync(userMenu);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[GenerateWeekMenu] Меню успешно создано с ID {userMenu.Id}");

                return Ok(new
                {
                    success = true,
                    menuId = userMenu.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GenerateWeekMenu] Error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/current")]
        public async Task<ActionResult> GetCurrentMenu(int userId)
        {
            try
            {
                var menu = await _context.Menus
                    .Include(m => m.Items)
                        .ThenInclude(i => i.Recipe)
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                if (menu == null)
                    return Ok(new
                    {
                        success = true,
                        data = (object)null,
                        message = "У пользователя нет выбранного меню"
                    });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Id = menu.Id,
                        Name = menu.Name,
                        Items = menu.Items.Select(i => new
                        {
                            RecipeId = i.RecipeId,
                            RecipeTitle = i.Recipe.Title,
                            Date = i.Date.ToString("yyyy-MM-dd"),
                            MealType = i.MealType
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/all")]
        public async Task<ActionResult> GetUserMenus(int userId)
        {
            try
            {
                var menus = await _context.Menus
                    .Include(i => i.Items)
                    .ThenInclude(u => u.Recipe)
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new MenuDto
                       {
                           Id = m.Id,
                           Name = m.Name,
                           CreatedAt = m.CreatedAt,
                           RecipeCount = m.Items.Count(),
                           Dates = m.Items.Select(i => i.Date.Date)
                       })
                    .ToListAsync();

                var result = menus.Select(m => new
                {
                    m.Id,
                    m.Name,
                    Description = $"Создано: {m.CreatedAt:dd.MM.yyyy}",
                    m.RecipeCount,
                    TotalDays = m.Dates.Distinct().Count()
                });

                return Ok(menus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetUserMenus] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{menuId}")]
        public async Task<ActionResult> GetMenuDetails(int menuId)
        {
            try
            {
                var menu = await _context.Menus
                    .Include(m => m.Items)
                        .ThenInclude(i => i.Recipe)
                    .FirstOrDefaultAsync(m => m.Id == menuId);

                if (menu == null)
                    return NotFound(new { error = "Меню не найдено" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Id = menu.Id,
                        Name = menu.Name,
                        CreatedAt = menu.CreatedAt,
                        Items = menu.Items.Select(i => new
                        {
                            RecipeId = i.RecipeId,
                            RecipeTitle = i.Recipe.Title,
                            Date = i.Date,
                            MealType = i.MealType
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetMenuDetails] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{menuId}")]
        public async Task<ActionResult> DeleteMenu(int menuId)
        {
            try
            {
                Console.WriteLine($"[DeleteMenu] Удаляем меню {menuId}");
                
                var menu = await _context.Menus
                    .Include(m => m.Items)
                    .FirstOrDefaultAsync(m => m.Id == menuId);

                if (menu == null)
                    return NotFound(new { error = "Меню не найдено" });

                _context.MenuItems.RemoveRange(menu.Items);
                _context.Menus.Remove(menu);
                
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[DeleteMenu] Меню {menuId} успешно удалено");
                
                return Ok(new
                {
                    success = true,
                    message = "Меню успешно удалено"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteMenu] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}