using Microsoft.AspNetCore.Mvc;
using YP_API.Interfaces;
using YP_API.Models;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;


namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly RecipePlannerContext _context;

        public RecipesController(IRecipeRepository recipeRepository, RecipePlannerContext context)
        {
            _recipeRepository = recipeRepository;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetRecipes()
        {
            try
            {
                var recipes = await _recipeRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    data = recipes.Select(r => new {
                        Id = r.Id,
                        Title = r.Title,
                        Description = r.Description,
                        Calories = r.Calories,
                        ImageUrl = r.ImageUrl
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }




        [HttpGet("{id}")]
        public async Task<ActionResult> GetRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                    return NotFound(new { error = "Рецепт не найден" });

                Console.WriteLine($"[GetRecipe] Загружаем рецепт {id}: {recipe.Title}");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Id = recipe.Id,
                        Title = recipe.Title,
                        Description = recipe.Description,
                        Instructions = recipe.Instructions,
                        PrepTime = recipe.PrepTime ?? 0,
                        CookTime = recipe.CookTime ?? 0,
                        Calories = recipe.Calories ?? 0,
                        ImageUrl = recipe.ImageUrl,
                        Ingredients = recipe.RecipeIngredients != null && recipe.RecipeIngredients.Count > 0
                            ? recipe.RecipeIngredients.Select(ri => new
                            {
                                Id = ri.IngredientId,
                                Name = ri.Ingredient?.Name ?? "Неизвестный ингредиент",
                                Quantity = ri.Quantity,
                                Unit = ri.Ingredient?.Unit ?? "шт"
                            }).Cast<object>().ToList()
                            : new List<object>()
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetRecipe] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("favorites/{userId}")]
        public async Task<ActionResult> GetFavorites(int userId)
        {
            try
            {
                var favorites = await _recipeRepository.GetUserFavoritesAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = favorites.Select(r => new {
                        Id = r.Id,
                        Title = r.Title,
                        Description = r.Description,
                        Calories = r.Calories,
                        ImageUrl = r.ImageUrl
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/favorite/{userId}")]
        public async Task<ActionResult> ToggleFavorite(int id, int userId)
        {
            try
            {
                var success = await _recipeRepository.ToggleFavoriteAsync(userId, id);

                return Ok(new
                {
                    success = true,
                    message = success ? "Добавлено в избранное" : "Удалено из избранного"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        [HttpGet("by-fridge/{userId}")]
        public async Task<ActionResult> GetRecipesByFridge(int userId)
        {
            try
            {
                var fridgeItems = await _context.FridgeItems
                    .Where(ui => ui.UserId == userId)
                    .ToListAsync();
                
                Console.WriteLine($"DEBUG: User {userId} has {fridgeItems.Count} items in fridge.");
                foreach (var item in fridgeItems)
                {
                    Console.WriteLine($"DEBUG: Item IngId={item.IngredientId}, Qty={item.Quantity}");
                }

                var fridgeIngredientIds = fridgeItems
                    .Select(ui => ui.IngredientId)
                    .Distinct()
                    .ToList();

                if (!fridgeIngredientIds.Any())
                {
                    Console.WriteLine("DEBUG: Fridge is empty!");
                    return Ok(new { success = true, data = Array.Empty<object>(), message = "В холодильнике нет ингредиентов" });
                }

                // СТРОГИЙ ПОИСК: Все ингредиенты рецепта должны быть в холодильнике
                var recipesWithAllIngredients = new List<Recipe>();
                
                var allRecipes = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .ToListAsync();

                foreach (var recipe in allRecipes)
                {
                    if (recipe.RecipeIngredients == null || recipe.RecipeIngredients.Count == 0)
                        continue;

                    // Проверяем ВСЕ ингредиенты рецепта
                    bool allIngredientsPresent = recipe.RecipeIngredients
                        .All(ri => fridgeIngredientIds.Contains(ri.IngredientId));

                    if (allIngredientsPresent)
                    {
                        Console.WriteLine($"? Рецепт '{recipe.Title}' - ВСЕ ингредиенты в наличии");
                        recipesWithAllIngredients.Add(recipe);
                    }
                    else
                    {
                        var missingIngredients = recipe.RecipeIngredients
                            .Where(ri => !fridgeIngredientIds.Contains(ri.IngredientId))
                            .Select(ri => ri.IngredientId)
                            .ToList();
                        Console.WriteLine($"? Рецепт '{recipe.Title}' - отсутствуют ингредиенты: {string.Join(", ", missingIngredients)}");
                    }
                }

                if (!recipesWithAllIngredients.Any())
                {
                    Console.WriteLine("DEBUG: No recipes with ALL ingredients found!");
                    return Ok(new 
                    { 
                        success = true, 
                        data = Array.Empty<object>(), 
                        message = "Нет рецептов со ВСЕМИ необходимыми ингредиентами" 
                    });
                }

                var result = recipesWithAllIngredients.Select(r => new
                {
                    Id = r.Id,
                    Title = r.Title,
                    Description = r.Description,
                    PrepTime = r.PrepTime,
                    CookTime = r.CookTime,
                    Calories = r.Calories,
                    ImageUrl = r.ImageUrl,
                    Ingredients = r.RecipeIngredients?.Select(ri => new
                    {
                        IngredientId = ri.IngredientId,
                        IngredientName = ri.Ingredient?.Name ?? "Unknown"
                    })
                });

                Console.WriteLine($"DEBUG: Found {recipesWithAllIngredients.Count} recipes with all ingredients");
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug/fridge-info/{userId}")]
        public async Task<ActionResult> GetFridgeDebugInfo(int userId)
        {
            try
            {
                var fridgeItems = await _context.FridgeItems
                    .Where(ui => ui.UserId == userId)
                    .Include(ui => ui.Ingredient)
                    .ToListAsync();

                var fridgeIds = fridgeItems.Select(fi => fi.IngredientId).Distinct().ToList();
                var fridgeNames = fridgeItems.Select(fi => fi.Ingredient?.Name?.ToLower()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

                var matched = await (from r in _context.Recipes
                                     join ri in _context.RecipeIngredients on r.Id equals ri.RecipeId
                                     join ing in _context.Ingredients on ri.IngredientId equals ing.Id
                                     where fridgeIds.Contains(ri.IngredientId) || fridgeNames.Contains(ing.Name.ToLower())
                                     select new
                                     {
                                         RecipeId = r.Id,
                                         RecipeTitle = r.Title,
                                         IngredientId = ri.IngredientId,
                                         IngredientName = ing.Name
                                     })
                                    .ToListAsync();

                var recipes = matched.Select(m => new { m.RecipeId, m.RecipeTitle }).Distinct().ToList();

                return Ok(new
                {
                    success = true,
                    fridge = fridgeItems.Select(fi => new { fi.IngredientId, Name = fi.Ingredient?.Name, fi.Quantity }),
                    matchedRecipes = recipes,
                    matchedDetails = matched
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
