namespace SecureBank.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string Message { get; set; } = "An unexpected error occurred while processing your request. Please try again or contact SecureBank customer support.";
    }
}
