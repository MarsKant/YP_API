using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using YP_API.Data;
using YP_API.Models;
using YP_API.Services;
using YP_API.Repositories;
using YP_API.Controllers;

namespace ApiTest
{
    [TestClass]
    public class UnitTest1
    {
        private RecipePlannerContext _context;
        private AuthService _authService;
        private RecipeRepository _recipeRepository;
        private UserRepository _userRepository;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<RecipePlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RecipePlannerContext(options);
            _userRepository = new UserRepository(_context);
            _recipeRepository = new RecipeRepository(_context);
            _authService = new AuthService(_userRepository);
        }

        [TestMethod]
        public async Task Test_User_Registration_Success()
        {
            string username = "qwe";
            string email = "admin@example.com";
            string password = "qweqwe";

            var user = await _authService.Register(username, email, password);

            Assert.IsNotNull(user, "Пользователь должен быть создан");
            Assert.AreEqual(username, user.Username, "Имя пользователя должно совпадать");
            Assert.AreEqual(email, user.Email, "Email должен совпадать");

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            Assert.IsNotNull(dbUser);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task Test_User_Login_WrongPassword()
        {
            await _authService.Register("user1", "user1@test.com", "correct_password");

            await _authService.Login("user1", "wrong_password");
        }

        [TestMethod]
        public async Task Test_User_Login_Success()
        {
            await _authService.Register("user2", "user2@test.com", "secure123");

            var user = await _authService.Login("user2", "secure123");

            Assert.IsNotNull(user);
            Assert.AreEqual("user2", user.Username);
        }

        [TestMethod]
        public async Task Test_ToggleFavorite_Add()
        {
            var user = new User { Id = 1, Username = "test", Password = "1", Email = "1" };
            var recipe = new Recipe { Id = 1, Title = "Borsch" };

            await _context.Users.AddAsync(user);
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            bool result = await _recipeRepository.ToggleFavoriteAsync(user.Id, recipe.Id);

            Assert.IsTrue(result, "Метод должен вернуть true при добавлении");
            var favorite = await _context.UserFavorites.FirstOrDefaultAsync();
            Assert.IsNotNull(favorite, "Запись должна появиться в таблице UserFavorites");
        }

        [TestMethod]
        public async Task Test_ShoppingList_Aggregation()
        {
            int userId = 1;

            var flour = new Ingredient
            {
                Id = 1,
                Name = "Мука",
                Unit = "г",
                Category = "Бакалея"
            };
            await _context.Ingredients.AddAsync(flour);

            var r1 = new Recipe { Id = 1, Title = "Pancakes" };
            var r2 = new Recipe { Id = 2, Title = "Bread" };
            await _context.Recipes.AddRangeAsync(r1, r2);

            await _context.RecipeIngredients.AddAsync(new RecipeIngredient { RecipeId = 1, IngredientId = 1, Quantity = 100, Ingredient = flour });
            await _context.RecipeIngredients.AddAsync(new RecipeIngredient { RecipeId = 2, IngredientId = 1, Quantity = 500, Ingredient = flour });

            var menu = new Menu { Id = 1, UserId = userId, Name = "Test Menu" };

            menu.Items.Add(new MenuItem
            {
                RecipeId = 1,
                MealType = "Завтрак",
                Date = DateTime.Today
            });

            menu.Items.Add(new MenuItem
            {
                RecipeId = 2,
                MealType = "Обед",
                Date = DateTime.Today
            });

            await _context.Menus.AddAsync(menu);
            await _context.SaveChangesAsync();

            var controller = new ShoppingListController(_context);
            var result = await controller.GenerateFromMenu(menu.Id, userId);

            var list = await _context.ShoppingLists.Include(sl => sl.Items).FirstOrDefaultAsync();
            Assert.IsNotNull(list);

            var flourItem = list.Items.FirstOrDefault(i => i.Name == "Мука");
            Assert.IsNotNull(flourItem);
            Assert.AreEqual(600, flourItem.Quantity, "Количество ингредиентов должно суммироваться");
        }
    }
}