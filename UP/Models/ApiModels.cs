using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace UP.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
    }

    public class RecipeDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int PrepTime { get; set; }
        public int CookTime { get; set; }
        public decimal Calories { get; set; }
        public List<IngredientDto> Ingredients { get; set; } = new List<IngredientDto>();
        
        [JsonProperty("instructions")]
        public string InstructionsRaw { get; set; }
        public List<string> Instructions
        {
            get
            {
                if (string.IsNullOrWhiteSpace(InstructionsRaw))
                    return new List<string>();
                
                if (InstructionsRaw.Contains("."))
                {
                    return InstructionsRaw.Split(new[] { '.', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }
                
                return new List<string> { InstructionsRaw };
            }
        }
        [NotMapped]
        public string DisplayImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImageUrl))
                {
                    return "pack://application:,,,/Images/receipt.png";
                }

                var bitmap = new BitmapImage(new Uri(ImageUrl, UriKind.Relative));
                bitmap.DecodeFailed += (s, e) =>
                {
                    ImageUrl = "pack://application:,,,/Images/receipt.png";
                };
                    

                return ImageUrl;
            }
        }
    }

    public class GigaChatApiResponse
    {
        public string Message { get; set; }
        public int MenuId { get; set; }
    }

    public class IngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
    }

    public class MenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal TotalCalories { get; set; }
        public List<MenuDayDto> Days { get; set; } = new List<MenuDayDto>();
    }

    public class MenuDayDto
    {
        public string Date { get; set; }
        public List<MenuMealDto> Meals { get; set; } = new List<MenuMealDto>();
    }

    public class MenuMealDto
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public string RecipeTitle { get; set; }
        public string MealType { get; set; }
        public decimal Calories { get; set; }
        public int PrepTime { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ShoppingListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ShoppingListItemDto> Items { get; set; } = new List<ShoppingListItemDto>();
    }

    public class ShoppingListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? Price { get; set; }
        public bool IsPurchased { get; set; }
    }

    public class AvailableMenu
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}