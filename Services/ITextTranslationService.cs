namespace SEN_T_PAZAR.Services;

public interface ITextTranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto", CancellationToken cancellationToken = default);
}
