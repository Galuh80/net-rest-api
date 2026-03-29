using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Product;
using RestAPI.Constantas.QueryParams;
using RestAPI.Services.Product;

namespace RestAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;

    public ProductController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiPagedResponse<Models.Product>>> GetPaged([FromQuery] ProductQueryParams query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(ApiPagedResponse<Models.Product>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Models.Product>>> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        if (product is null)
            return NotFound(ApiResponse<Models.Product>.Fail($"Product with id '{id}' not found"));

        return Ok(ApiResponse<Models.Product>.Ok(product));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Models.Product>>> Create([FromForm] CreateProductDto dto)
    {
        try
        {
            var product = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id },
                ApiResponse<Models.Product>.Ok(product, "Product created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ApiResponse<Models.Product>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Models.Product>>> Update(Guid id, [FromForm] UpdateProductDto dto)
    {
        try
        {
            var product = await _service.UpdateAsync(id, dto);
            if (product is null)
                return NotFound(ApiResponse<Models.Product>.Fail($"Product with id '{id}' not found"));

            return Ok(ApiResponse<Models.Product>.Ok(product, "Product updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ApiResponse<Models.Product>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail($"Product with id '{id}' not found"));

        return Ok(ApiResponse<object>.Ok(null!, "Product deleted successfully"));
    }
}
