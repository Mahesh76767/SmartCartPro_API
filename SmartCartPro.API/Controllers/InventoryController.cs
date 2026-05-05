using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Models.Common;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        // TODO: Inject IInventoryService and implement endpoints
        [HttpGet]
        public IActionResult Get() =>
            Ok(ApiResponse<string>.Ok("Inventory module - implement IInventoryService next"));
    }
}