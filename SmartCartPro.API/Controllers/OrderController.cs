using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Order;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
       public readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] OrderFilterDto dto)
        {
            var result = await _service.GetAllAsync(dto);
            return Ok(ApiResponse<PagedResult<OrderResponseDto>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if(result == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Order {id} not found"));
            }
            return Ok(ApiResponse<OrderResponseDto>.Ok(result));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<int>.Ok(id, "Order placed successfully"));
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles ="Admin,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            await _service.UpdateStatusAsync(id, dto);
            return Ok(ApiResponse<string>.Ok("Order cancelled. Stock restored for all items."));
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderDto dto)
        {
            await _service.CancelAsync(id, dto);
            return Ok(ApiResponse<string>.Ok("Order cancelled. Stock restored for all items."));
        }

        [HttpPost("validate-discount")]
        public async Task<IActionResult> ValidateDiscount([FromBody] ValidateDiscountDto dto)
        {
            var result = await _service.ValidateDiscountAsync(dto.Code);
            return Ok(ApiResponse<DiscountResponseDto>.Ok(result));
        }

    }
}