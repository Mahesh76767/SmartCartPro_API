using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Models.Common;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PurchaseOrderController : ControllerBase
    {
        // TODO: Inject IPurchaseOrderService and implement endpoints
        [HttpGet]
        public IActionResult Get() =>
            Ok(ApiResponse<string>.Ok("PurchaseOrder module - implement IPurchaseOrderService next"));
    }
}