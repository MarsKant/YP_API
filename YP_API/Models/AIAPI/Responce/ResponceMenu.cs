namespace YP_API.Models.AIAPI.Responce
{
    public class ResponceMenu
    {
        public class GeneratedMenuDto
        {
            public string MenuName { get; set; }
            public List<GeneratedMenuItemDto> Items { get; set; }
        }

        public class GeneratedMenuItemDto
        {
            public int DayNumber { get; set; } 
            public string MealType { get; set; } 
            public GeneratedRecipeDto Recipe { get; set; }
        }

        public class GeneratedRecipeDto
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public List<string> Instructions{ get; set; }
            public int Calories { get; set; }
            public int PrepTime { get; set; }
            public int CookTime { get; set; }
            public List<GeneratedIngredientDto> Ingredients { get; set; }
        }

        public class GeneratedIngredientDto
        {
            public string Name { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
            public string Category { get; set; }
        }
    }
}
