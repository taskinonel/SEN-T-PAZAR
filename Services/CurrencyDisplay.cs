namespace SEN_T_PAZAR.Services;

public interface ICurrencyRateProvider
{
    Task<Dictionary<string, decimal>> GetRatesAsync(CancellationToken cancellationToken = default);
}

public class HardcodedCurrencyRateProvider : ICurrencyRateProvider
{
    // Fallback values only. In production, replace with an API provider.
    private static readonly Dictionary<string, decimal> FallbackRates = new()
    {
        { "USD", 32m },
        { "EUR", 35m },
        { "GBP", 41m }
    };

    public Task<Dictionary<string, decimal>> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FallbackRates);
    }
}

public static class CurrencyDisplay
{
    public static string Format(decimal amount, string? currency)
    {
        var normalizedCurrency = Normalize(currency);
        if (string.IsNullOrWhiteSpace(normalizedCurrency) || normalizedCurrency == "TL")
            return $"₺{amount:N0}";
        return $"{amount:N0} {normalizedCurrency}";
    }

    public static async Task<string> FormatWithTryEquivalentAsync(decimal amount, string? currency, ICurrencyRateProvider rateProvider)
    {
        var normalizedCurrency = Normalize(currency);
        if (string.IsNullOrWhiteSpace(normalizedCurrency) || normalizedCurrency == "TL")
            return $"₺{amount:N0}";

        var rates = await rateProvider.GetRatesAsync();
        if (rates.TryGetValue(normalizedCurrency, out var rate))
        {
            var tryAmount = amount * rate;
            return $"{amount:N0} {normalizedCurrency} (₺{tryAmount:N0})";
        }
        return $"{amount:N0} {normalizedCurrency}";
    }

    public static string Normalize(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return "TL";
        }

        var normalized = currency.Trim().ToUpperInvariant();
        return normalized switch
        {
            "TRY" => "TL",
            "₺" => "TL",
            _ => normalized
        };
    }
}
