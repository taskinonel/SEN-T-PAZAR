namespace SEN_T_PAZAR.Services;

public interface ISmsSender
{
    Task<(bool Success, string? ErrorMessage)> SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
