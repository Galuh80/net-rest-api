using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Product;
using RestAPI.Constantas.QueryParams;
using RestAPI.Infrastructures;
using RestAPI.Services.Webhook;

namespace RestAPI.Services.Product;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IWebhookService _webhook;
    private readonly IDistributedCache _cache;
    private const string CachePrefix = "product:";

    public ProductService(AppDbContext context, IWebHostEnvironment env, IWebhookService webhook, IDistributedCache cache)
    {
        _context = context;
        _env = env;
        _webhook = webhook;
        _cache = cache;
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync($"{CachePrefix}all");
    }

    private async Task<string?> SaveImageAsync(IFormFile? image)
    {
        if (image is null) return null;

        var uploadsPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "products");
        Directory.CreateDirectory(uploadsPath);

        var ext = Path.GetExtension(image.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private void DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", imageUrl.TrimStart('/'));
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public async Task<PagedResult<Models.Product>> GetPagedAsync(ProductQueryParams query)
    {
        var cacheKey = $"{CachePrefix}all:{query.Page}:{query.PageSize}:{query.Search}:{query.CategoryId}:{query.MinPrice}:{query.MaxPrice}:{query.SortBy}:{query.SortOrder}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<PagedResult<Models.Product>>(cached)!;

        var q = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.MinPrice.HasValue)
            q = q.Where(p => p.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(p => p.Price <= query.MaxPrice.Value);

        q = query.SortBy.ToLower() switch
        {
            "price"     => query.SortOrder == "desc" ? q.OrderByDescending(p => p.Price)     : q.OrderBy(p => p.Price),
            "createdat" => query.SortOrder == "desc" ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            "updatedat" => query.SortOrder == "desc" ? q.OrderByDescending(p => p.UpdatedAt) : q.OrderBy(p => p.UpdatedAt),
            _           => query.SortOrder == "desc" ? q.OrderByDescending(p => p.Name)      : q.OrderBy(p => p.Name)
        };

        var totalCount = await q.CountAsync();
        var data = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        var result = new PagedResult<Models.Product>
        {
            Data = data,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        return result;
    }

    public async Task<Models.Product?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{CachePrefix}{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<Models.Product>(cached);

        var product = await _context.Products.FindAsync(id);
        if (product is not null)
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        return product;
    }

    public async Task<Models.Product> CreateAsync(CreateProductDto dto)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category with id '{dto.CategoryId}' not found");

        var product = new Models.Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            ImageUrl = await SaveImageAsync(dto.Image),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        await InvalidateCacheAsync();

        await _webhook.SendAsync("product.created", product);

        return product;
    }

    public async Task<Models.Product?> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return null;

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category with id '{dto.CategoryId}' not found");

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        if (dto.Image is not null)
        {
            DeleteImage(product.ImageUrl);
            product.ImageUrl = await SaveImageAsync(dto.Image);
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"{CachePrefix}{id}");
        await InvalidateCacheAsync();

        await _webhook.SendAsync("product.updated", product);

        return product;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return false;

        DeleteImage(product.ImageUrl);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"{CachePrefix}{id}");
        await InvalidateCacheAsync();

        await _webhook.SendAsync("product.deleted", new { product.Id, product.Name });

        return true;
    }
}
