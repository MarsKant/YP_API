using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Models;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly RecipePlannerContext _context;

        public GameController(RecipePlannerContext context)
        {
            _context = context;
        }

        // Получить очки и уровень пользователя
        [HttpGet("user/{userId}/points")]
        public async Task<ActionResult> GetUserPoints(int userId)
        {
            try
            {
                var userPoints = await _context.Set<UserPoints>()
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        Points = 0,
                        Level = 1,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.Set<UserPoints>().Add(userPoints);
                    await _context.SaveChangesAsync();
                }

                int nextLevelPoints = (userPoints.Level + 1) * 1000;
                double progress = (userPoints.Points % 1000) / 10.0;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        points = userPoints.Points,
                        level = userPoints.Level,
                        nextLevelPoints = nextLevelPoints,
                        progress = progress
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Начисление очков за добавление продукта
        [HttpPost("user/{userId}/product-added")]
        public async Task<ActionResult> ProductAdded(int userId)
        {
            return await AddPoints(userId, 10, "product_added");
        }

        // Начисление очков за удаление продукта
        [HttpPost("user/{userId}/product-removed")]
        public async Task<ActionResult> ProductRemoved(int userId)
        {
            return await AddPoints(userId, 5, "product_removed");
        }

        // Начисление очков за создание меню
        [HttpPost("user/{userId}/menu-created")]
        public async Task<ActionResult> MenuCreated(int userId)
        {
            return await AddPoints(userId, 50, "menu_created");
        }

        // Начисление очков за добавление в избранное
        [HttpPost("user/{userId}/favorite-added")]
        public async Task<ActionResult> FavoriteAdded(int userId)
        {
            return await AddPoints(userId, 15, "favorite_added");
        }

        // Начисление очков за просмотр рецепта
        [HttpPost("user/{userId}/recipe-viewed")]
        public async Task<ActionResult> RecipeViewed(int userId)
        {
            return await AddPoints(userId, 3, "recipe_viewed");
        }

        // Начисление очков за генерацию списка покупок
        [HttpPost("user/{userId}/shopping-list-generated")]
        public async Task<ActionResult> ShoppingListGenerated(int userId)
        {
            return await AddPoints(userId, 20, "shopping_list_generated");
        }

        // Общий метод для начисления очков
        private async Task<ActionResult> AddPoints(int userId, int pointsToAdd, string action)
        {
            try
            {
                var userPoints = await _context.Set<UserPoints>()
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        Points = 0,
                        Level = 1
                    };
                    _context.Set<UserPoints>().Add(userPoints);
                }

                userPoints.Points += pointsToAdd;

                // Расчет уровня (каждые 1000 очков = новый уровень)
                int newLevel = (userPoints.Points / 1000) + 1;
                bool leveledUp = newLevel > userPoints.Level;

                if (leveledUp)
                {
                    userPoints.Level = newLevel;
                }

                userPoints.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                string message = leveledUp
                    ? $"🎉 УРОВЕНЬ {userPoints.Level}! +{pointsToAdd} очков за {GetActionName(action)}"
                    : $"+{pointsToAdd} очков за {GetActionName(action)}";

                int nextLevelPoints = (userPoints.Level + 1) * 1000;
                double progress = (userPoints.Points % 1000) / 10.0;

                return Ok(new
                {
                    success = true,
                    message = message,
                    data = new
                    {
                        points = userPoints.Points,
                        level = userPoints.Level,
                        leveledUp = leveledUp,
                        nextLevelPoints = nextLevelPoints,
                        progress = progress
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string GetActionName(string action)
        {
            return action switch
            {
                "product_added" => "добавление продукта",
                "product_removed" => "удаление продукта",
                "menu_created" => "создание меню",
                "favorite_added" => "добавление в избранное",
                "recipe_viewed" => "просмотр рецепта",
                "shopping_list_generated" => "генерацию списка покупок",
                _ => action
            };
        }
    }
}