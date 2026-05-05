using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var result = await _categoryService.GetCategories();
                return Ok(ApiResponse<List<Category>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetCategoriesById(int Id)
        {
            try
            {
                var result = await _categoryService.GetCategoryById(Id);
                if (result == null)
                    return NotFound(ApiResponse<object>.Fail($"Category {Id} not found"));
                return Ok(ApiResponse<Category>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] Category dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<object>.Fail("Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

                var id = await _categoryService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetCategoriesById), new { id },
                    ApiResponse<int>.Ok(id, "Category created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] Category dto)
        {
            var result = await _categoryService.UpdateAsync(id, dto);

            if (!result)
                return NotFound(ApiResponse<string>.Fail("Category not found"));

            return Ok(ApiResponse<string>.Ok("Category updated successfully"));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok("Category deleted successfully"));
        }
    }
}