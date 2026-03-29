using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Product;
using RestAPI.Constantas.QueryParams;

namespace RestAPI.Services.Product;

public interface IProductService
{
    Task<PagedResult<Models.Product>> GetPagedAsync(ProductQueryParams query);
    Task<Models.Product?> GetByIdAsync(Guid id);
    Task<Models.Product> CreateAsync(CreateProductDto dto);
    Task<Models.Product?> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteAsync(Guid id);
}
