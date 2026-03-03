using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using YP_API.Data;
using YP_API.Interfaces;
using YP_API.Models;
using YP_API.Services;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngredientsController : ControllerBase
    {
        private readonly RecipePlannerContext _context;
        private readonly IPriceParserService _priceParserService;
        private readonly ILogger<IngredientsController> _logger;
        private readonly IServiceProvider _serviceProvider;

        public IngredientsController(
            RecipePlannerContext context, 
            IPriceParserService priceParserService, 
            ILogger<IngredientsController> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _priceParserService = priceParserService;
            _logger = logger;
            _serviceProvider = serviceProvider;
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
                    query = query.Where(i => i.Name.Contains(lower, StringComparison.CurrentCultureIgnoreCase));
                }

                var ingredients = await query
                    .Take(50)
                    .ToListAsync();

                if (ingredients.Count == 0 && !string.IsNullOrWhiteSpace(name))
                {
                    var existing = await _context.Ingredients
                        .FirstOrDefaultAsync(i => i.Name.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase));

                    ingredients.Add(existing);
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

        [HttpGet("get")]
        public async Task<ActionResult> GetIngredientByName([FromQuery] string? name)
        {
            try
            {
                var lower = name.ToLower();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest();
                }

                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(
                    x => x.Name == lower);

                if (ingredient != null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = ingredient
                    });
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateIngredients([FromBody] List<IngredientDto> dtos)
        {
            try
            {
                if (dtos == null || !dtos.Any())
                    return BadRequest(new { success = false, error = "Список ингредиентов пуст" });

                var created = new List<object>();
                var skipped = new List<object>();
                var errors = new List<object>();

                foreach (var dto in dtos)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(dto.Name))
                        {
                            errors.Add(new { name = dto.Name, error = "Имя не может быть пустым" });
                            continue;
                        }

                        var existing = await _context.Ingredients
                            .FirstOrDefaultAsync(i => i.Name.Equals(dto.Name.Trim(), StringComparison.CurrentCultureIgnoreCase));

                        if (existing != null)
                        {
                            skipped.Add(new { Id = existing.Id, Name = existing.Name, reason = "Уже существует" });
                            continue;
                        }

                        var newIngredient = new Ingredient
                        {
                            Name = dto.Name.Trim(),
                            Unit = dto.Unit ?? "шт",
                            Category = dto.Category ?? "Разное",
                            Price = null
                        };

                        _context.Ingredients.Add(newIngredient);
                        created.Add(new { Id = newIngredient.Id, Name = newIngredient.Name });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new { name = dto.Name, error = ex.Message });
                    }
                }

                await _context.SaveChangesAsync();

                if (created.Any())
                {
                    var newIngredients = dtos
                        .Where(d => !string.IsNullOrWhiteSpace(d.Name))
                        .Select(d => new IngredientDto { Name = d.Name.Trim(), Category = d.Category, Unit = d.Unit})
                        .ToList();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var scopedParser = scope.ServiceProvider.GetRequiredService<IPriceParserService>();
                            await scopedParser.ParsePriceAsync(newIngredients);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Ошибка парсинга цен при создании ингредиентов");
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        created = created,
                        skipped = skipped,
                        errors = errors,
                        summary = new
                        {
                            total = dtos.Count,
                            createdCount = created.Count,
                            skippedCount = skipped.Count,
                            errorCount = errors.Count
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при массовом создании ингредиентов");
                return StatusCode(500, new { success = false, error = ex.Message });
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
        public string Unit {  get; set; }
    }
}
