using SmartCartPro.Models.DTOs.AI;
namespace SmartCartPro.Business.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateProductDescriptionAsync(GenerateDescriptionDto dto);
        Task<SentimentResponseDto> AnalyzeSentimentAsync(string text);
        Task<InvoiceExtractDto> AnalyzeInvoiceAsync(string extractedText);
        Task<string> GetOrderInsightsAsync(string context);
        Task<string> GetSmartAlertsAsync(string salesContext);
    }
}