namespace YP_API.Interfaces
{
    public interface IPriceParserService
    {
        Task<double> ParsePriceAsync(string productName, string? volume = null);
    }
}
