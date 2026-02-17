using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Helpers;
using YP_API.Models;
namespace YP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly RecipePlannerContext _context;

        public AiController(RecipePlannerContext context)
        {
            _context = context;
        }

        [HttpPost("ask/{userId}")]
        public async Task<IActionResult> AskGigaChat(int userId, [FromBody] List<Ingredient> Ingredients)
        {
            if (Ingredients == null || !Ingredients.Any())
            {
                return BadRequest("Запрос не может быть пустым.");
            }

            try
            {
                var token = await GigaChatHelper.GetToken();
                var generatedMenu = await GigaChatHelper.GenerateAndParseMenuAsync(token, Ingredients, 1);

                if (generatedMenu == null) return StatusCode(502, "Ошибка генерации меню.");

                var menuEntity = new Menu
                {
                    UserId = userId,
                    Name = generatedMenu.MenuName,
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<MenuItem>()
                };

                DateTime startDate = DateTime.Today;
                var localIngredientCache = new Dictionary<string, Ingredient>();

                foreach (var itemDto in generatedMenu.Items)
                {
                    var recipeEntity = new Recipe
                    {
                        Title = itemDto.Recipe.Title,
                        Description = itemDto.Recipe.Description,
                        Instructions = string.Join("\n", itemDto.Recipe.Instructions),
                        Calories = itemDto.Recipe.Calories,
                        PrepTime = itemDto.Recipe.PrepTime,
                        CookTime = itemDto.Recipe.CookTime,
                        RecipeIngredients = new List<RecipeIngredient>()
                    };

                    foreach (var ingDto in itemDto.Recipe.Ingredients)
                    {
                        var ingName = ingDto.Name.Trim();
                        var ingKey = ingName.ToLower(); 

                        Ingredient ingredientEntity;

                        if (localIngredientCache.ContainsKey(ingKey))
                        {
                            ingredientEntity = localIngredientCache[ingKey];
                        }
                        else
                        {
                            ingredientEntity = await _context.Ingredients
                                .FirstOrDefaultAsync(i => i.Name == ingName);

                            if (ingredientEntity == null)
                            {
                                ingredientEntity = new Ingredient
                                {
                                    Name = ingName,
                                    Unit = ingDto.Unit,
                                    Category = ingDto.Category
                                };
                                _context.Ingredients.Add(ingredientEntity);
                            }
                            localIngredientCache[ingKey] = ingredientEntity;
                        }

                        recipeEntity.RecipeIngredients.Add(new RecipeIngredient
                        {
                            Ingredient = ingredientEntity,
                            Quantity = ingDto.Quantity
                        });
                    }

                    var menuItemEntity = new MenuItem
                    {
                        Date = startDate.AddDays(itemDto.DayNumber - 1),
                        MealType = itemDto.MealType,
                        Recipe = recipeEntity
                    };
                    menuEntity.Items.Add(menuItemEntity);
                }

                _context.Menus.Add(menuEntity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Меню успешно сгенерировано",
                    MenuId = menuEntity.Id,
                    Result = generatedMenu
                });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "";
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message} {innerMessage}");
            }
        }

        [HttpGet("test-generation-real")]
        public async Task<IActionResult> TestGenerationWithMockData()
        {
            try
            {
                var mockIngredients = new List<Ingredient>
                {
                    new Ingredient { Name = "Куриная грудка", Unit = "кг" },
                    new Ingredient { Name = "Рис", Unit = "кг" },
                    new Ingredient { Name = "Помидоры", Unit = "шт" },
                    new Ingredient { Name = "Сметана", Unit = "г" },
                    new Ingredient { Name = "Чеснок", Unit = "зуб" }
                };

                string token = await GigaChatHelper.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return StatusCode(500, "Ошибка получения токена (Token is null)");
                }

                var generatedMenu = await GigaChatHelper.GenerateAndParseMenuAsync(token, mockIngredients, 1);

                if (generatedMenu == null)
                {
                    return StatusCode(502, "GigaChat вернул пустой ответ или ошибка парсинга.");
                }

                return Ok(new
                {
                    Message = "Меню успешно сгенерировано на основе тестовых продуктов",
                    InputIngredients = mockIngredients.Select(i => i.Name),
                    Result = generatedMenu
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка: {ex.Message}");
            }
        }
    }
}