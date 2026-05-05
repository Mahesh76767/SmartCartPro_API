using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Product;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly IProductRepository _repo;

        public ProductController(IProductService service, IProductRepository repo)
        {
            _service = service;
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(ApiResponse<PagedResult<ProductResponseDto>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<ProductResponseDto>.Ok(result));
        }


        [HttpPost]
        [Authorize(Roles = ("Admin,Manager"))]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id },
                ApiResponse<int>.Ok(id, "Product created successfully"));
        }


        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<string>.Ok("Product updated successfully"));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok("Product deleted successfully"));

        }


        [HttpPost("{id:int}/generate-description")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GenerateDescription(int id)
        {
            var description = await _service.GenerateDescriptionAsync(id);
            return Ok(ApiResponse<string>.Ok(description, "Description generated"));
        }
    }
}
    