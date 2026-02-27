using System.ComponentModel.DataAnnotations.Schema;

namespace YP_API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; } = false;
    }
    public class FridgeItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public User User { get; set; } = null!;
        public Ingredient Ingredient { get; set; } = null!;
    }


    public class RecipeDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public int? Calories { get; set; }
        public string? ImageUrl { get; set; }
    }
    public class Recipe
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public decimal? Calories { get; set; }
        public string? ImageUrl { get; set; } = "";
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }

    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public decimal? Price {  get; set; }
    }

    public class RecipeIngredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }

        public Recipe Recipe { get; set; }
        public Ingredient Ingredient { get; set; }
    }

    public class Menu
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public List<MenuItem> Items { get; set; } = new List<MenuItem>();
    }

    public class MenuItem
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public int RecipeId { get; set; }
        public DateTime Date { get; set; }
        public string MealType { get; set; }
        public Menu Menu { get; set; }
        public Recipe Recipe { get; set; }
    }

    public class ShoppingList
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public ICollection<ShoppingListItem> Items { get; set; } = new List<ShoppingListItem>();
    }

    public class ShoppingListItem
    {
        public int Id { get; set; }
        public int ShoppingListId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public bool IsPurchased { get; set; }
        public Ingredient Ingredient { get; set; }
        public ShoppingList ShoppingList { get; set; }
    }

    public class UserFavorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RecipeId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Recipe Recipe { get; set; }
    }
}