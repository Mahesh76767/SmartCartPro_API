using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.Models.DTOs.AI;
using Microsoft.Extensions.DependencyInjection;

namespace SmartCartPro.Business.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _http;
        private readonly string _ollamaUrl;
        private readonly string _ollamaModel;
        private readonly string _hfApiKey;
        private const string HF_BASE = "https://api-inference.huggingface.co/models/";

        public AIService(System.Net.Http.IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _http = httpClientFactory.CreateClient("AI");
            _ollamaUrl = config["AISettings:OllamaUrl"] ?? "http://localhost:11434/api/generate";
            _ollamaModel = config["AISettings:OllamaModel"] ?? "llama3";
            _hfApiKey = config["AISettings:HuggingFaceApiKey"] ?? string.Empty;
        }

        private async Task<string> CallOllamaAsync(string prompt)
        {
            var payload = new { model = _ollamaModel, prompt, stream = false };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_ollamaUrl, content);
            response.EnsureSuccessStatusCode();
            var raw = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        }

        public async Task<string> GenerateProductDescriptionAsync(GenerateDescriptionDto dto)
        {
            var prompt = $"Write a short e-commerce product description (2-3 sentences) for: {dto.ProductName}. Category: {dto.Category ?? "General"}. Features: {dto.KeyFeatures ?? "N/A"}. Return ONLY the description text.";
            return await CallOllamaAsync(prompt);
        }

        public async Task<InvoiceExtractDto> AnalyzeInvoiceAsync(string extractedText)
        {
            var prompt = $"Extract invoice data from this text as JSON with fields: VendorName, InvoiceNumber, InvoiceDate, DueDate, SubTotal, TaxAmount, TotalAmount, LineItems (array of Description/Quantity/UnitPrice/Total). Return ONLY valid JSON. Text: {extractedText}";
            var result = await CallOllamaAsync(prompt);
            try
            {
                var clean = result.Trim().TrimStart('`').TrimEnd('`');
                if (clean.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                    clean = clean[4..].Trim();
                return JsonSerializer.Deserialize<InvoiceExtractDto>(clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new InvoiceExtractDto();
            }
            catch { return new InvoiceExtractDto { VendorName = "Parse error - check raw response" }; }
        }

        public async Task<SentimentResponseDto> AnalyzeSentimentAsync(string text)
        {
            try
            {
                var model = "distilbert-base-uncased-finetuned-sst-2-english";
                var payload = new { inputs = text };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{HF_BASE}{model}");
                request.Content = content;
                if (!string.IsNullOrEmpty(_hfApiKey))
                    request.Headers.Add("Authorization", $"Bearer {_hfApiKey}");

                var resp = await _http.SendAsync(request);
                var raw = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(raw);
                var arr = doc.RootElement[0].EnumerateArray().ToList();
                var top = arr.OrderByDescending(x => x.GetProperty("score").GetDecimal()).First();
                return new SentimentResponseDto
                {
                    Label = top.GetProperty("label").GetString() ?? "UNKNOWN",
                    Score = top.GetProperty("score").GetDecimal()
                };
            }
            catch { return new SentimentResponseDto { Label = "NEUTRAL", Score = 0.5m }; }
        }

        public async Task<string> GetOrderInsightsAsync(string context)
        {
            var prompt = $"You are a retail analytics assistant. Analyze this sales data and give 3 key insights as a numbered list: {context}";
            return await CallOllamaAsync(prompt);
        }

        public async Task<string> GetSmartAlertsAsync(string salesContext)
        {
            var prompt = $"Based on this retail data, generate 2-3 smart business alerts as JSON array: [{{\"title\":\"string\",\"message\":\"string\",\"type\":\"warning|info|success\"}}]. Data: {salesContext}. Return ONLY JSON array."; return await CallOllamaAsync(prompt);
        }
    }
}