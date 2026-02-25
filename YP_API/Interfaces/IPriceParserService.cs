using YP_API.Controllers;

namespace YP_API.Interfaces
{
    public interface IPriceParserService
    {
        Task<bool> ParsePriceAsync(List<IngredientDto> ingredients);
    }
}
