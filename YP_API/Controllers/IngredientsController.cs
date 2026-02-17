using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Models;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngredientsController : ControllerBase
    {
        private readonly RecipePlannerContext _context;

        public IngredientsController(RecipePlannerContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<ActionResult> SearchIngredientsFromQuery([FromQuery] string? name, [FromQuery] string? searchName)
        {
            string queryText = searchName ?? name;

            try
            {
                IQueryable<Ingredient> query = _context.Ingredients;

                if (!string.IsNullOrWhiteSpace(queryText))
                {
                    var lower = queryText.ToLower();
                    query = query.Where(i => i.Name.ToLower().Contains(lower));
                }

                var ingredients = await query
                    .Take(50)
                    .ToListAsync();

                if (!ingredients.Any() && !string.IsNullOrWhiteSpace(name))
                {
                    var existing = await _context.Ingredients
                        .FirstOrDefaultAsync(i => i.Name.ToLower() == name.Trim().ToLower());

                    if (existing == null)
                    {
                        var newIngredient = new Ingredient
                        {
                            Name = name.Trim(),
                            Category = "Разное",
                            Unit = "шт"
                        };

                        _context.Ingredients.Add(newIngredient);
                        await _context.SaveChangesAsync();

                        ingredients.Add(newIngredient);
                    }
                    else
                    {
                        ingredients.Add(existing);
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = ingredients.Select(i => new
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Category = i.Category,
                        Unit = i.Unit
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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

                if (!fridgeItems.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        data = new object[0]
                    });
                }

                var result = fridgeItems.Select(fi => new
                {
                    Id = fi.IngredientId,
                    Name = fi.ProductName,       
                    Category = fi.Ingredient?.Category ?? "Неизвестно",
                    Unit = fi.Unit,
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

        [HttpPost("create")]
        public async Task<IActionResult> CreateIngredient([FromBody] IngredientDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { success = false, error = "Имя не может быть пустым" });

                var existing = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Name.ToLower() == dto.Name.Trim().ToLower());

                if (existing != null)
                    return Ok(new { success = true, data = new { existing.Id, existing.Name } });

                var newIngredient = new Ingredient
                {
                    Name = dto.Name.Trim(),
                    Unit = "шт",
                    Category = existing.Category
                };

                _context.Ingredients.Add(newIngredient);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = new { newIngredient.Id, newIngredient.Name } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Ingredients
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = categories
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class IngredientDto
    {
        public string Name { get; set; }
        public string Category { get; set; }
    }
}
