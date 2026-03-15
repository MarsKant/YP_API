using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace YP_API.Services
{
    public class ImageGenerationService : IImageGenerationService
    {
        private readonly ILogger<ImageGenerationService> _logger;
        string token;
        public ImageGenerationService(IConfiguration configuration, ILogger<ImageGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateImageUrlAsync(string prompt)
        {
            try
            {
                using var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);

                var token = await Helpers.GigaChatHelper.GetToken();

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var requestBody = new
                {
                    model = "GigaChat",
                    messages = new[]
                    {
                        new { role = "system", content = "Ты - профессиональный художник. Создавай качественные изображения блюд." },
                        new { role = "user", content = prompt }
                    },
                    function_call = "auto"
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://gigachat.devices.sberbank.ru/api/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"GigaChat API error: {response.StatusCode}, body: {error}");
                    return null;
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonConvert.DeserializeObject<Models.ImageApi.Response.Response.ChatCompletionResponse>(responseJson);

                if (chatResponse?.choices?.Count > 0)
                {
                    string contentText = chatResponse.choices[0]?.message?.content;
                    if (!string.IsNullOrEmpty(contentText))
                    {
                        var match = Regex.Match(contentText, @"<img\s+src=""([^""]+)""\s+fuse=""true""\s*/>");
                        if (match.Success)
                        {
                            return await DownloadImage(match.Groups[1].Value);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации изображения");
                return null;
            }
        }
        private async Task<string> DownloadImage(string fileId)
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);

                var token = await Helpers.GigaChatHelper.GetToken();

                client.DefaultRequestHeaders.Add("Accept", "image/jpeg");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                string url = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Ошибка загрузки: {response.StatusCode}");
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (imageBytes.Length == 0)
                {
                    throw new Exception("Получен пустой файл");
                }

                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string imagesPath = Path.Combine(webRootPath, "images");

                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                string fileName = $"wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string filePath = Path.Combine(imagesPath, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                Console.WriteLine($"Файл сохранен: {filePath}");

                string baseUrl = "http://10.0.2.2:5286";
                return $"{baseUrl}/images/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                return null;
            }
        }
    }
}