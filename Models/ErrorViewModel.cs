namespace SEN_T_PAZAR.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string? ErrorDetail { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool ShowErrorDetail => !string.IsNullOrEmpty(ErrorDetail);
}
