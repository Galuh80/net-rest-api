using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Category;
using RestAPI.Constantas.QueryParams;
using RestAPI.Infrastructures;
using RestAPI.Services.Webhook;

namespace RestAPI.Services.Category;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    private readonly IWebhookService _webhook;
    private readonly IDistributedCache _cache;
    private const string CachePrefix = "category:";

    public CategoryService(AppDbContext context, IWebhookService webhook, IDistributedCache cache)
    {
        _context = context;
        _webhook = webhook;
        _cache = cache;
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync($"{CachePrefix}all");
    }

    public async Task<PagedResult<Models.Category>> GetPagedAsync(CategoryQueryParams query)
    {
        var cacheKey = $"{CachePrefix}all:{query.Page}:{query.PageSize}:{query.Search}:{query.SortBy}:{query.SortOrder}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<PagedResult<Models.Category>>(cached)!;

        var q = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(c => c.Name.ToLower().Contains(query.Search.ToLower()));

        q = query.SortBy.ToLower() switch
        {
            "createdat" => query.SortOrder == "desc" ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
            "updatedat" => query.SortOrder == "desc" ? q.OrderByDescending(c => c.UpdatedAt) : q.OrderBy(c => c.UpdatedAt),
            _ => query.SortOrder == "desc" ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name)
        };

        var totalCount = await q.CountAsync();
        var data = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        var result = new PagedResult<Models.Category>
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

    public async Task<Models.Category?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{CachePrefix}{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<Models.Category>(cached);

        var category = await _context.Categories.FindAsync(id);
        if (category is not null)
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(category),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        return category;
    }

    public async Task<Models.Category> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Models.Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        await InvalidateCacheAsync();

        await _webhook.SendAsync("category.created", category);

        return category;
    }

    public async Task<Models.Category?> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is null) return null;

        category.Name = dto.Name;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"{CachePrefix}{id}");
        await InvalidateCacheAsync();

        await _webhook.SendAsync("category.updated", category);

        return category;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is null) return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"{CachePrefix}{id}");
        await InvalidateCacheAsync();

        await _webhook.SendAsync("category.deleted", new { category.Id, category.Name });

        return true;
    }
}
