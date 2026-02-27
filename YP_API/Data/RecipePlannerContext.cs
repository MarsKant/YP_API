using Microsoft.EntityFrameworkCore;
using YP_API.Models;

namespace YP_API.Data
{
    public static class DataSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "qwe", Password = "qweqwe", Email = "admin@example.com", IsAdmin = true },
                new User { Id = 2, Username = "user", Password = "123456", Email = "user@example.com" }
            );

            modelBuilder.Entity<Ingredient>().HasData(
                new Ingredient { Id = 1, Name = "Яйца", Category = "Молочные продукты", Unit = "шт" },
                new Ingredient { Id = 2, Name = "Молоко", Category = "Молочные продукты", Unit = "мл" },
                new Ingredient { Id = 3, Name = "Мука", Category = "Бакалея", Unit = "г" },
                new Ingredient { Id = 4, Name = "Сахар", Category = "Бакалея", Unit = "г" },
                new Ingredient { Id = 5, Name = "Соль", Category = "Специи", Unit = "г" },
                new Ingredient { Id = 6, Name = "Перец", Category = "Специи", Unit = "г" },
                new Ingredient { Id = 7, Name = "Куриная грудка", Category = "Мясо", Unit = "г" },
                new Ingredient { Id = 8, Name = "Рис", Category = "Крупы", Unit = "г" },
                new Ingredient { Id = 9, Name = "Картофель", Category = "Овощи", Unit = "г" },
                new Ingredient { Id = 10, Name = "Лук", Category = "Овощи", Unit = "г" },
                new Ingredient { Id = 11, Name = "Морковь", Category = "Овощи", Unit = "г" },
                new Ingredient { Id = 12, Name = "Помидоры", Category = "Овощи", Unit = "г" },
                new Ingredient { Id = 13, Name = "Оливковое масло", Category = "Масла", Unit = "мл" },
                new Ingredient { Id = 14, Name = "Сливочное масло", Category = "Молочные продукты", Unit = "г" },
                new Ingredient { Id = 15, Name = "Чеснок", Category = "Овощи", Unit = "г" }
            );

            modelBuilder.Entity<Recipe>().HasData(
                new Recipe
                {
                    Id = 1,
                    Title = "Омлет с овощами",
                    Description = "Легкий и полезный завтрак",
                    Instructions = "1. Взбить яйца с молоком\n2. Добавить нарезанные овощи\n3. Жарить на среднем огне 5-7 минут",
                    Calories = 250,
                    PrepTime = 10,
                    CookTime = 10,
                    ImageUrl = "/images/omelette.jpg"
                },
                new Recipe
                {
                    Id = 2,
                    Title = "Курица с рисом",
                    Description = "Сытное основное блюдо",
                    Instructions = "1. Обжарить курицу\n2. Добавить овощи\n3. Варить рис 20 минут",
                    Calories = 450,
                    PrepTime = 15,
                    CookTime = 30,
                    ImageUrl = "/images/chicken_rice.jpg"
                },
                new Recipe
                {
                    Id = 3,
                    Title = "Картофельное пюре",
                    Description = "Классический гарнир",
                    Instructions = "1. Отварить картофель\n2. Растолочь с молоком и маслом\n3. Добавить соль по вкусу",
                    Calories = 200,
                    PrepTime = 10,
                    CookTime = 20,
                    ImageUrl = "/images/mashed_potatoes.jpg"
                }
            );
            modelBuilder.Entity<RecipeIngredient>().HasData(
                new RecipeIngredient { Id = 1, RecipeId = 1, IngredientId = 1, Quantity = 3 }, // Яйца
                new RecipeIngredient { Id = 2, RecipeId = 1, IngredientId = 2, Quantity = 50 }, // Молоко
                new RecipeIngredient { Id = 3, RecipeId = 1, IngredientId = 5, Quantity = 5 }, // Соль
                new RecipeIngredient { Id = 4, RecipeId = 1, IngredientId = 6, Quantity = 2 }, // Перец
                new RecipeIngredient { Id = 5, RecipeId = 1, IngredientId = 10, Quantity = 50 }, // Лук
                new RecipeIngredient { Id = 6, RecipeId = 1, IngredientId = 12, Quantity = 100 }, // Помидоры

                new RecipeIngredient { Id = 7, RecipeId = 2, IngredientId = 7, Quantity = 300 }, // Куриная грудка
                new RecipeIngredient { Id = 8, RecipeId = 2, IngredientId = 8, Quantity = 200 }, // Рис
                new RecipeIngredient { Id = 9, RecipeId = 2, IngredientId = 10, Quantity = 100 }, // Лук
                new RecipeIngredient { Id = 10, RecipeId = 2, IngredientId = 11, Quantity = 100 }, // Морковь
                new RecipeIngredient { Id = 11, RecipeId = 2, IngredientId = 13, Quantity = 30 }, // Оливковое масло
                new RecipeIngredient { Id = 12, RecipeId = 2, IngredientId = 5, Quantity = 10 }, // Соль

                new RecipeIngredient { Id = 13, RecipeId = 3, IngredientId = 9, Quantity = 500 }, // Картофель
                new RecipeIngredient { Id = 14, RecipeId = 3, IngredientId = 2, Quantity = 100 }, // Молоко
                new RecipeIngredient { Id = 15, RecipeId = 3, IngredientId = 14, Quantity = 50 }, // Сливочное масло
                new RecipeIngredient { Id = 16, RecipeId = 3, IngredientId = 5, Quantity = 10 } // Соль
            );

            modelBuilder.Entity<UserFavorite>().HasData(
                new UserFavorite { Id = 1, UserId = 1, RecipeId = 1, AddedAt = DateTime.UtcNow.AddDays(-5) },
                new UserFavorite { Id = 2, UserId = 1, RecipeId = 2, AddedAt = DateTime.UtcNow.AddDays(-3) },
                new UserFavorite { Id = 3, UserId = 2, RecipeId = 3, AddedAt = DateTime.UtcNow.AddDays(-1) }
            );

            modelBuilder.Entity<ShoppingList>().HasData(
                new ShoppingList
                {
                    Id = 1,
                    UserId = 1,
                    Name = "Еженедельные покупки",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                },
                new ShoppingList
                {
                    Id = 2,
                    UserId = 2,
                    Name = "Для ужина",
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            );
        }
    }

    public class RecipePlannerContext : DbContext
    {
        public RecipePlannerContext(DbContextOptions<RecipePlannerContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }

        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<FridgeItem> FridgeItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Recipe>().ToTable("recipes");
            modelBuilder.Entity<Ingredient>().ToTable("ingredients");
            modelBuilder.Entity<RecipeIngredient>().ToTable("recipe_ingredients");
            modelBuilder.Entity<Menu>().ToTable("menus");
            modelBuilder.Entity<MenuItem>().ToTable("menu_items");
            modelBuilder.Entity<ShoppingList>().ToTable("shopping_lists");
            modelBuilder.Entity<ShoppingListItem>().ToTable("shopping_list_items");
            modelBuilder.Entity<UserFavorite>().ToTable("user_favorites");
            modelBuilder.Entity<FridgeItem>().ToTable("fridge_items");

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).HasMaxLength(100);
                entity.HasIndex(u => u.Username).IsUnique();
            });

            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Description).HasMaxLength(1000);
                entity.Property(r => r.Instructions).HasColumnType("text");
                entity.Property(r => r.ImageUrl).HasMaxLength(500);
                entity.Property(r => r.Calories).HasPrecision(10, 2);
            });

            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(i => i.Name).IsUnique();
            });

            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.HasKey(uf => uf.Id);
                entity.HasIndex(uf => new { uf.UserId, uf.RecipeId }).IsUnique();
            });



            modelBuilder.Entity<FridgeItem>(entity =>
            {
                entity.HasKey(fi => fi.Id);
                entity.HasIndex(fi => new { fi.UserId, fi.IngredientId }).IsUnique();
            });

            DataSeeder.Seed(modelBuilder);
        }
    }
}