namespace SmartCartPro.Models.DTOs.AI
{
    public class AIRequestDto
    {
        public string Prompt { get; set; } = string.Empty;
        public string? Context { get; set; }
    }
    public class GenerateDescriptionDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? KeyFeatures { get; set; }
    }
    public class SentimentRequestDto
    {
        public string Text { get; set; } = string.Empty;
    }
}