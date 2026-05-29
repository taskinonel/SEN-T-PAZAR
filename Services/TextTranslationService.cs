using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SEN_T_PAZAR.Services;

public sealed class TextTranslationService : ITextTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public TextTranslationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(8);
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
        {
            return text;
        }

        var normalizedTarget = NormalizeLanguage(targetLanguage);
        var normalizedSource = NormalizeLanguage(sourceLanguage);

        if (normalizedTarget == normalizedSource)
        {
            return text;
        }

        var cacheKey = $"{normalizedSource}:{normalizedTarget}:{text}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        string translated = string.Empty;
        try
        {
            translated = await TryLibreTranslateAsync(text, normalizedSource, normalizedTarget, cancellationToken);
            if (string.IsNullOrWhiteSpace(translated) || LooksUntranslated(text, translated, normalizedTarget))
            {
                translated = await TryMyMemoryAsync(text, normalizedSource, normalizedTarget, cancellationToken);
            }
        }
        catch
        {
            // ignore, fallback below
        }

        // Fallback: Eğer çeviri başarısızsa veya boşsa, Türkçe (orijinal) metni döndür
        if (string.IsNullOrWhiteSpace(translated))
        {
            translated = text;
        }

        Cache[cacheKey] = translated;
        return translated;
    }

    private async Task<string> TryLibreTranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        var endpoint = _configuration["Translation:Endpoint"]?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = "https://translate.argosopentech.com/translate";
        }

        var apiKey = _configuration["Translation:ApiKey"];

        try
        {
            var payload = new Dictionary<string, string>
            {
                ["q"] = text,
                ["source"] = string.IsNullOrWhiteSpace(sourceLanguage) ? "auto" : sourceLanguage,
                ["target"] = targetLanguage,
                ["format"] = "text"
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                payload["api_key"] = apiKey;
            }

            using var content = new FormUrlEncodedContent(payload);
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken: cancellationToken);
            return result?.TranslatedText?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<string> TryMyMemoryAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedSource = sourceLanguage == "auto" ? "tr" : sourceLanguage;
            var escaped = WebUtility.UrlEncode(text);
            var url = $"https://api.mymemory.translated.net/get?q={escaped}&langpair={normalizedSource}|{targetLanguage}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var payload = await response.Content.ReadFromJsonAsync<MyMemoryResponse>(cancellationToken: cancellationToken);
            return payload?.ResponseData?.TranslatedText?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool LooksUntranslated(string source, string translated, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(translated))
        {
            return true;
        }

        if (string.Equals(source.Trim(), translated.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (source.Length < 6)
            {
                return false;
            }

            return targetLanguage is "en" or "ru" or "ar" or "fa";
        }

        return false;
    }

    private static string NormalizeLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "auto";
        }

        return code.Trim().ToLowerInvariant() switch
        {
            "tr-tr" => "tr",
            "en-us" => "en",
            "en-gb" => "en",
            "ru-ru" => "ru",
            "ar-sa" => "ar",
            var x when x.Contains('-') => x.Split('-')[0],
            var x => x
        };
    }

    private sealed class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
    }

    private sealed class MyMemoryResponse
    {
        [JsonPropertyName("responseData")]
        public MyMemoryData? ResponseData { get; set; }
    }

    private sealed class MyMemoryData
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
    }
}
