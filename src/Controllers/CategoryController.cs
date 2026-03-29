using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Category;
using RestAPI.Constantas.QueryParams;
using RestAPI.Services.Category;

namespace RestAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiPagedResponse<Models.Category>>> GetPaged([FromQuery] CategoryQueryParams query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(ApiPagedResponse<Models.Category>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Models.Category>>> GetById(Guid id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category is null)
            return NotFound(ApiResponse<Models.Category>.Fail($"Category with id '{id}' not found"));

        return Ok(ApiResponse<Models.Category>.Ok(category));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Models.Category>>> Create([FromBody] CreateCategoryDto dto)
    {
        var category = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            ApiResponse<Models.Category>.Ok(category, "Category created successfully"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Models.Category>>> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _service.UpdateAsync(id, dto);
        if (category is null)
            return NotFound(ApiResponse<Models.Category>.Fail($"Category with id '{id}' not found"));

        return Ok(ApiResponse<Models.Category>.Ok(category, "Category updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail($"Category with id '{id}' not found"));

        return Ok(ApiResponse<object>.Ok(null!, "Category deleted successfully"));
    }
}
