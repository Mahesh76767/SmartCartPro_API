using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Customer;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;
        public CustomerController(ICustomerService service) { _service = service; }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CustomerFilterDto filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(ApiResponse<PagedResult<CustomerResponseDto>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(ApiResponse<object>.Fail($"Customer {id} not found"));
            return Ok(ApiResponse<CustomerResponseDto>.Ok(result));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed", 
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<int>.Ok(id, "Customer created"));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult>Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<string>.Ok("Customer updated successfully"));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok("Customer deleted successfully"));
        }

        [HttpGet("{id:int}/orders")]
        public async Task<IActionResult> GetOrders(int id)
        {
           var result = await _service.GetOrdersAsync(id);
            return Ok(ApiResponse<List<CustomerOrderDto>>.Ok(result));
        }

        [HttpGet("{id:int}/reviews")]
        public async Task<IActionResult> GetReviews(int id)
        {
            var result = await _service.GetReviewsAsync(id);
            return Ok(ApiResponse<List<ReviewResponseDto>>.Ok(result));
        }

        [HttpPost("{id:int}/review")]
        public async Task<IActionResult> AddReview(int id, [FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var result = await _service.AddReviewAsync(id, dto);
            return Ok(ApiResponse<ReviewResponseDto>.Ok(result, "Review added successfully"));
        }
    }
}