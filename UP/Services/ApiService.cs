using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UP.Models;

namespace UP.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _token;

        public ApiService(string baseUrl = "https://localhost:7197/")
        {
            _baseUrl = baseUrl;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public void SetToken(string token)
        {
            _token = token;
            if (!string.IsNullOrEmpty(_token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        public void ClearToken()
        {
            _token = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<UserData> LoginAsync(string username, string password)
        {
            try
            {
                var formData = new MultipartFormDataContent
                {
                    { new StringContent(username), "username" },
                    { new StringContent(password), "password" }
                };
                var response = await _httpClient.PostAsync("api/auth/login", formData);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка входа: {response.StatusCode}");

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObj == null) throw new Exception("Пустой ответ");

                bool success = responseObj.success ?? false;
                if (!success) throw new Exception(responseObj.message?.ToString() ?? "Ошибка");

                var userData = new UserData
                {
                    Id = responseObj.id ?? responseObj.data?.id ?? 0,
                    Username = responseObj.username ?? responseObj.data?.username ?? username,
                    Email = responseObj.email ?? responseObj.data?.email ?? "",
                    Token = responseObj.token ?? responseObj.data?.token ?? "",
                    IsAdmin = responseObj.isAdmin ?? false,
                };

                SetToken(userData.Token);
                return userData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Login error: {ex.Message}");
            }
        }
        public async Task<UserData> RegisterAsync(string username, string email, string password)
        {
            try
            {
                var formData = new MultipartFormDataContent
                {
                    { new StringContent(username), "username" },
                    { new StringContent(email), "email" },
                    { new StringContent(password), "password" }
                };

                var response = await _httpClient.PostAsync("api/auth/register", formData);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка регистрации: {response.StatusCode}");

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                bool success = responseObj?.success ?? false;
                if (!success) throw new Exception(responseObj?.message?.ToString() ?? "Ошибка регистрации");

                // Обычно после регистрации сразу логинимся или возвращаем UserData
                return new UserData { Username = username, Email = email };
            }
            catch (Exception ex)
            {
                throw new Exception($"Registration error: {ex.Message}");
            }
        }

        // --- Users ---

        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/users/get");
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return new List<UserDto>();

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObj?.data != null)
                {
                    return JsonConvert.DeserializeObject<List<UserDto>>(responseObj.data.ToString()) ?? new List<UserDto>();
                }
                return new List<UserDto>();
            }
            catch { return new List<UserDto>(); }
        }

        // --- Recipes ---

        public async Task<List<RecipeDto>> GetRecipesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/recipes");
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return new List<RecipeDto>();

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObj?.data != null)
                {
                    return JsonConvert.DeserializeObject<List<RecipeDto>>(responseObj.data.ToString()) ?? new List<RecipeDto>();
                }
                return new List<RecipeDto>();
            }
            catch { return new List<RecipeDto>(); }
        }
        public async Task<RecipeDto> GetRecipeAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/recipes/{id}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return null;

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObj?.data != null)
                    return JsonConvert.DeserializeObject<RecipeDto>(responseObj.data.ToString());

                return null;
            }
            catch { return null; }
        }

        public async Task<List<RecipeDto>> GetFavoritesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/recipes/favorites/{AppData.CurrentUser.Id}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return new List<RecipeDto>();

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObj?.data != null)
                    return JsonConvert.DeserializeObject<List<RecipeDto>>(responseObj.data.ToString()) ?? new List<RecipeDto>();

                return new List<RecipeDto>();
            }
            catch { return new List<RecipeDto>(); }
        }

        public async Task<bool> ToggleFavoriteAsync(int recipeId)
        {
            try
            {
                int userId = AppData.CurrentUser?.Id ?? 0;
                if (userId == 0) return false;

                var response = await _httpClient.PostAsync($"api/recipes/{recipeId}/favorite/{userId}", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<RecipeDto>> GetRecipesByFridgeAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/recipes/by-fridge/{userId}");
                if (!response.IsSuccessStatusCode) return new List<RecipeDto>();

                var str = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(str);

                if (obj?.data != null)
                    return JsonConvert.DeserializeObject<List<RecipeDto>>(obj.data.ToString()) ?? new List<RecipeDto>();

                return new List<RecipeDto>();
            }
            catch { return new List<RecipeDto>(); }
        }

        public async Task<List<AvailableMenu>> GetUserMenusAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/menu/user/{userId}/all");
                if (!response.IsSuccessStatusCode) return new List<AvailableMenu>();

                var responseString = await response.Content.ReadAsStringAsync();
                var menus = JsonConvert.DeserializeObject<List<AvailableMenu>>(responseString);
                return menus ?? new List<AvailableMenu>();
            }
            catch { return new List<AvailableMenu>(); }
        }

        public async Task<MenuDto> GetCurrentMenuAsync()
        {
            try
            {
                int userId = AppData.CurrentUser?.Id ?? 0;
                if (userId == 0) return null;

                var response = await _httpClient.GetAsync($"api/menu/user/{userId}/current");
                if (!response.IsSuccessStatusCode) return null;

                var str = await response.Content.ReadAsStringAsync();

                var responseObj = JsonConvert.DeserializeObject<ApiResponse<MenuBackendData>>(str);
                if (responseObj?.Data != null) return MapBackendMenuToFrontend(responseObj.Data);

                return null;
            }
            catch { return null; }
        }

        public async Task<MenuDto> GetMenuDetailsAsync(int menuId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/menu/{menuId}");
                if (!response.IsSuccessStatusCode) return null;

                var str = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<ApiResponse<MenuBackendData>>(str);

                if (responseObj?.Data != null)
                {
                    return MapBackendMenuToFrontend(responseObj.Data);
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> DeleteMenuAsync(int menuId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/menu/{menuId}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }


        public async Task<MenuDto> GenerateMenuFromGigaChatAsync(int userId, List<IngredientDto> ingredients)
        {
            try
            {
                var jsonIngredients = JsonConvert.SerializeObject(ingredients);
                var content = new StringContent(jsonIngredients, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/Ai/ask/{userId}", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return null;

                var apiResponse = JsonConvert.DeserializeObject<GigaChatApiResponse>(responseString);
                if (apiResponse == null || apiResponse.MenuId == 0) return null;

                var finalMenu = await GetMenuDetailsAsync(apiResponse.MenuId);

                if (finalMenu != null)
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var day in finalMenu.Days)
                        {
                            foreach (var meal in day.Meals)
                            {
                                await ParseImageForRecipe(meal.RecipeId, meal.RecipeTitle);
                            }
                        }
                    });
                }

                return finalMenu;
            }
            catch { return null; }
        }

        private async Task ParseImageForRecipe(int recipeId, string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                    return;

                var requestData = new
                {
                    RecipeId = recipeId,
                    query = title  
                };

                string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("api/images/generate", content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка генерации изображения для рецепта {recipeId}: {response.StatusCode}, тело: {errorBody}");
                }

                //if (string.IsNullOrWhiteSpace(title)) return;
                //string query = Uri.EscapeDataString(title);
                //await _httpClient.GetAsync($"api/images/generate?RecipeId={recipeId}&query={query}");
                //await _httpClient.GetAsync($"api/images/parse-povar?RecipeId={recipeId}&query={query}");
            }
            catch { }
        }

        public async Task<ShoppingListDto> GetCurrentShoppingListAsync()
        {
            try
            {
                int userId = AppData.CurrentUser?.Id ?? 0;
                if (userId == 0) return null;

                var response = await _httpClient.GetAsync($"api/shoppinglist/user/{userId}/current");
                if (!response.IsSuccessStatusCode) return null;

                var str = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(str);

                if (obj?.data != null)
                    return JsonConvert.DeserializeObject<ShoppingListDto>(obj.data.ToString());

                return null;
            }
            catch { return null; }
        }

        public async Task<bool> GenerateShoppingListAsync(int menuId)
        {
            try
            {
                int userId = AppData.CurrentUser?.Id ?? 0;
                if (userId == 0) return false;

                var response = await _httpClient.PostAsync($"api/shoppinglist/generate-from-menu/{menuId}/{userId}", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }


        public async Task<List<IngredientDto>> SearchIngredientsAsync(string term)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/ingredients/search?name={Uri.EscapeDataString(term)}");
                if (response.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    var obj = JsonConvert.DeserializeObject<dynamic>(str);
                    if (obj?.data != null)
                        return JsonConvert.DeserializeObject<List<IngredientDto>>(obj.data.ToString()) ?? new List<IngredientDto>();
                }
            }
            catch { }
            return new List<IngredientDto>();
        }

        public async Task<bool> AddFridgeItem(int userId, IngredientDto productName)
        {
            try
            {
                var json = JsonConvert.SerializeObject(productName);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/inventory/FridgeItem/add/{userId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка {response.StatusCode}: {errorText}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddFridgeItem error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<IngredientDto>> UpdateFridgeAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/ingredients/fridge/{userId}");
                var str = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(str);
                if (obj?.data != null)
                    return JsonConvert.DeserializeObject<List<IngredientDto>>(obj.data.ToString());
                return new List<IngredientDto>();
            }
            catch { return null; }
        }

        public async Task<bool> RemoveFridgeAsync(int userId, int ingredientId) {
            try {
            var response = await _httpClient.DeleteAsync($"api/inventory/user/{userId}/ingredient/{ingredientId}");
                return response.IsSuccessStatusCode;
            }
            catch {
                return false;
            }
        }

        

        public async Task<int?> FindIngredientIdByNameAsync(string name)
        {
            try
            {
                var url = $"api/ingredients/search?name={Uri.EscapeDataString(name)}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var str = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(str);
                if (obj?.data != null && obj.data.Count > 0) return (int)obj.data[0].id;
                return null;
            }
            catch { return null; }
        }

        public async Task<int?> CreateIngredientByNameAsync(string name)
        {
            try
            {
                var newIng = new { Name = name };
                string json = System.Text.Json.JsonSerializer.Serialize(newIng);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/ingredients/create", content);
                var str = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(str);

                if (obj?.success == true && obj.data != null) return (int)obj.data.id;
                return null;
            }
            catch { return null; }
        }


        private MenuDto MapBackendMenuToFrontend(MenuBackendData backendData)
        {
            var menuDto = new MenuDto
            {
                Id = backendData.Id,
                Name = backendData.Name,
                Days = new List<MenuDayDto>()
            };

            if (backendData.Items != null && backendData.Items.Count > 0)
            {
                var grouped = backendData.Items.GroupBy(i => i.Date.Date).OrderBy(g => g.Key);
                foreach (var group in grouped)
                {
                    var dayDto = new MenuDayDto
                    {
                        Date = group.Key.ToString("yyyy-MM-dd"),
                        Meals = new List<MenuMealDto>()
                    };
                    foreach (var item in group)
                    {
                        dayDto.Meals.Add(new MenuMealDto
                        {
                            RecipeId = item.RecipeId,
                            RecipeTitle = item.RecipeTitle,
                            MealType = item.MealType,
                            ImageUrl = "",
                            Calories = 0,
                            PrepTime = 0
                        });
                    }
                    menuDto.Days.Add(dayDto);
                }
            }
            return menuDto;
        }


        private class MenuBackendData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<MenuBackendItem> Items { get; set; }
        }

        private class MenuBackendItem
        {
            public int RecipeId { get; set; }
            public string RecipeTitle { get; set; }
            public DateTime Date { get; set; }
            public string MealType { get; set; }
        }
    }
}