using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using YP_API.Models;
using YP_API.Models.AIAPI;
using static YP_API.Models.AIAPI.Responce.ResponceMenu;

namespace YP_API.Helpers
{
    public static class GigaChatHelper
    {
        public static string ClientId = "019bca1f-0f50-72b2-b33b-7fb5c3b89be6";
        public static string AuthorizationKey = "MDE5YmNhMWYtMGY1MC03MmIyLWIzM2ItN2ZiNWMzYjg5YmU2OjE2NjQxYWQ0LWVhMjctNDYzYi1hYjRmLTRjZTI4ZDU1NTVkOA==";

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        private static string _cachedToken = "";
        private static DateTime _tokenExpiry = DateTime.MinValue;

        /// <summary>
        /// Основной метод генерации меню с повторными попытками
        /// </summary>
        public static async Task<GeneratedMenuDto?> GenerateAndParseMenuAsync(List<Ingredient> ingredients, int daysCount)
        {
            var finalMenu = new GeneratedMenuDto
            {
                MenuName = "Сгенерированное меню",
                Items = new List<GeneratedMenuItemDto>()
            };

            for (int i = 1; i <= daysCount; i++)
            {
                bool daySuccess = false;
                string systemPrompt = CreateSingleDayPrompt(ingredients, i);

                var messages = new List<Request.Message>
                {
                    new Request.Message { role = "user", content = systemPrompt }
                };

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        string token = await GetToken();
                        var response = await GetAnswer(token, messages);

                        if (response?.choices?.Count > 0)
                        {
                            string rawContent = response.choices[0].message.content;
                            string json = CleanJson(rawContent);

                            var dayMenu = JsonConvert.DeserializeObject<GeneratedMenuDto>(json);

                            if (dayMenu?.Items != null && dayMenu.Items.Any())
                            {
                                foreach (var item in dayMenu.Items) item.DayNumber = i;

                                finalMenu.Items.AddRange(dayMenu.Items);
                                daySuccess = true;
                                break; 
                            }
                        }

                        Console.WriteLine($"Попытка {attempt} для дня {i} не удалась (пустой ответ или неверный формат).");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка генерации дня {i} (Попытка {attempt}): {ex.Message}");
                        await Task.Delay(1500);
                    }
                }

                if (!daySuccess)
                {
                    Console.WriteLine($"Не удалось сгенерировать меню для дня {i} после 3 попыток.");
                }
            }

            return finalMenu.Items.Count > 0 ? finalMenu : null;
        }

        /// <summary>
        /// Выполняет запрос к API GigaChat
        /// </summary>
        public static async Task<Models.AIAPI.Responce.ResponseMessage?> GetAnswer(string token, List<Request.Message> messages)
        {
            string url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            var requestData = new
            {
                model = "GigaChat",
                stream = false,
                repetition_penalty = 1,
                messages = messages,
                temperature = 0.25
            };

            var jsonContent = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Создаем сообщение запроса (HttpClient используется общий, но сообщение создаем новое)
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Client-ID", ClientId); // Иногда требуют этот заголовок
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error ({response.StatusCode}): {errorBody}");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Models.AIAPI.Responce.ResponseMessage>(responseContent);
        }

        /// <summary>
        /// Получает токен, используя кеш и автообновление
        /// </summary>
        public static async Task<string> GetToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
            {
                return _cachedToken;
            }

            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
            string rqUid = Guid.NewGuid().ToString(); 

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", AuthorizationKey);
            request.Headers.Add("RqUID", rqUid);

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            };
            request.Content = new FormUrlEncodedContent(formData);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Не удалось получить токен. Status: {response.StatusCode}. Details: {error}");
                }

                string responseString = await response.Content.ReadAsStringAsync();
                var tokenData = JsonConvert.DeserializeObject<dynamic>(responseString);

                _cachedToken = tokenData.access_token;
                long expiresAt = tokenData.expires_at;
                _tokenExpiry = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).UtcDateTime;

                return _cachedToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL AUTH ERROR: {ex.Message}");
                throw;
            }
        }

        private static string CleanJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";
            json = json.Replace("```json", "").Replace("```", "").Trim();

            int firstBrace = json.IndexOf('{');
            int lastBrace = json.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                json = json.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return json;
        }

        public static string CreateSingleDayPrompt(List<Ingredient> ingredients, int dayNumber)
        {
            string ingredientsString = string.Join(", ", ingredients.Select(i => $"\"{i.Name}\""));

            return $@"
Ты — профессиональный шеф-повар и нутрициолог. Твоя задача — составить меню на ДЕНЬ №{dayNumber}, используя только предоставленные продукты.

ВХОДНЫЕ ДАННЫЕ:
Продукты в наличии: {ingredientsString}

СТРОГИЕ ПРАВИЛА (ОБЯЗАТЕЛЬНО К ИСПОЛНЕНИЮ):
1. Верни ТОЛЬКО валидный JSON. Никакого Markdown, никаких объяснений, никакого текста до или после JSON.
2. Меню должно содержать 3 приема пищи: Завтрак, Обед, Ужин.
3. В рецептах используй преимущественно продукты из списка ""ВХОДНЫЕ ДАННЫЕ"".
4. КАЖДЫЙ ингредиент в рецепте ДОЛЖЕН содержать поле ""category"".
   - Это критически важно для работы приложения.
   - Если категория не очевидна, определи её сам (варианты: ""Овощи"", ""Фрукты"", ""Мясо"", ""Рыба"", ""Молочные"", ""Крупы"", ""Яйца"", ""Специи"", ""Другое"").
5. Все ключи в JSON должны быть на английском языке и в нижнем регистре (camelCase), как в примере ниже.

ПРИМЕР ПРАВИЛЬНОГО ОТВЕТА (ОБРАЗЕЦ):
{{
  ""items"": [
    {{
      ""dayNumber"": {dayNumber},
      ""mealType"": ""Завтрак"",
      ""recipe"": {{
        ""title"": ""Омлет с овощами"",
        ""description"": ""Пышный омлет со свежими помидорами"",
        ""calories"": 350,
        ""prepTime"": 10,
        ""cookTime"": 15,
        ""instructions"": [
          ""Взбейте яйца с солью"",
          ""Обжарьте овощи"",
          ""Залейте яйцами и готовьте под крышкой""
        ],
        ""ingredients"": [
          {{
            ""name"": ""Яйца"",
            ""quantity"": 3,
            ""unit"": ""шт"",
            ""category"": ""Яйца""
          }},
          {{
            ""name"": ""Помидоры"",
            ""quantity"": 100,
            ""unit"": ""г"",
            ""category"": ""Овощи""
          }}
        ]
      }}
    }},
    {{
      ""dayNumber"": {dayNumber},
      ""mealType"": ""Обед"",
      ""recipe"": {{ ... }}
    }},
    {{
      ""dayNumber"": {dayNumber},
      ""mealType"": ""Ужин"",
      ""recipe"": {{ ... }}
    }}
  ]
}}

Генерируй меню строго по этому формату:";
        }
    }
}