using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Models.Common;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        // TODO: Inject IInvoiceService and implement endpoints
        [HttpGet]
        public IActionResult Get() =>
            Ok(ApiResponse<string>.Ok("Invoice module - implement IInvoiceService next"));
    }
}