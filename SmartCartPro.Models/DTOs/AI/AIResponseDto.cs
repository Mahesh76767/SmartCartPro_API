namespace SmartCartPro.Models.DTOs.AI
{
    public class AIResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
    public class SentimentResponseDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Score { get; set; }
    }
}