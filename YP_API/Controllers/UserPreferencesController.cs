using Microsoft.AspNetCore.Mvc;
using YP_API.Data;
using YP_API.Models;

[ApiController]
[Route("api/[controller]")]
public class UserPreferencesController : ControllerBase
{
    private readonly RecipePlannerContext _context;
    public UserPreferencesController(RecipePlannerContext context) => _context = context;

    //[HttpPost("user/{userId}/fridge")]
    //public async Task<IActionResult> SetFridge(int userId, [FromBody] List<FridgeItemDto> items)
    //{
    //    Console.WriteLine("SetFridge (to UserInventories) items: " +
    //        string.Join(", ", items.Select(i => $"{i.IngredientId}:{i.Quantity}")));

    //    var user = await _context.Users.FindAsync(userId);
    //    if (user == null) return NotFound(new { error = "User not found" });

    //    var existing = _context.FridgeItems.Where(f => f.UserId == userId);
    //    _context.FridgeItems.RemoveRange(existing);

    //    var validItems = items
    //        .Where(i => i.IngredientId > 0) 
    //        .ToList();

    //    foreach (var item in validItems)
    //        _context.FridgeItems.Add(new FridgeItem
    //        {
    //            UserId = userId,
    //            IngredientId = item.IngredientId,
    //            Quantity = (decimal)item.Quantity,
    //            Unit = "шт"
    //        });

    //    await _context.SaveChangesAsync();
    //    return Ok(new { success = true });
    //}


    public class FridgeItemDto
    {
        public int IngredientId { get; set; }
        public double Quantity { get; set; }
    }
}
