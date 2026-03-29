using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Category;
using RestAPI.Constantas.QueryParams;
using RestAPI.Models;

namespace RestAPI.Services.Category;

public interface ICategoryService
{
    Task<PagedResult<Models.Category>> GetPagedAsync(CategoryQueryParams query);
    Task<Models.Category?> GetByIdAsync(Guid id);
    Task<Models.Category> CreateAsync(CreateCategoryDto dto);
    Task<Models.Category?> UpdateAsync(Guid id, UpdateCategoryDto dto);
    Task<bool> DeleteAsync(Guid id);
}
