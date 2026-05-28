namespace Computational_Practice.Common
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ValidationErrorResponse : ErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}
