using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Models.Common;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        // TODO: Inject IReportService and implement endpoints
        [HttpGet]
        public IActionResult Get() =>
            Ok(ApiResponse<string>.Ok("Report module - implement IReportService next"));
    }
}