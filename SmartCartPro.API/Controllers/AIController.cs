using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.AI;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIService _ai;

        public AIController(IAIService ai)
        {
            _ai = ai;
        }

        [HttpPost("generate-description")]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionDto dto)
        {
            var result = await _ai.GenerateProductDescriptionAsync(dto);
            return Ok(ApiResponse<string>.Ok(result));
        }

        [HttpPost("analyze-invoice")]
        public async Task<IActionResult> AnalyzeInvoice([FromBody] AIRequestDto dto)
        {
            var result = await _ai.AnalyzeInvoiceAsync(dto.Prompt);
            return Ok(ApiResponse<InvoiceExtractDto>.Ok(result));
        }

        [HttpPost("analyze-sentiment")]
        public async Task<IActionResult> AnalyzeSentiment([FromBody] SentimentRequestDto dto)
        {
            var result = await _ai.AnalyzeSentimentAsync(dto.Text);
            return Ok(ApiResponse<SentimentResponseDto>.Ok(result));
        }

        [HttpPost("order-insights")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetOrderInsights([FromBody] AIRequestDto dto)
        {
            var result = await _ai.GetOrderInsightsAsync(dto.Context ?? dto.Prompt);
            return Ok(ApiResponse<string>.Ok(result));
        }

        [HttpPost("smart-alerts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSmartAlerts([FromBody] AIRequestDto dto)
        {
            var result = await _ai.GetSmartAlertsAsync(dto.Context ?? dto.Prompt);
            return Ok(ApiResponse<string>.Ok(result));
        }
    }
}