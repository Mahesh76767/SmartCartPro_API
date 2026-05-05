using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Models.Common;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        // TODO: Inject ISupplierService and implement endpoints
        [HttpGet]
        public IActionResult Get() =>
            Ok(ApiResponse<string>.Ok("Supplier module - implement ISupplierService next"));
    }
}