using RestAPI.Constantas;

namespace RestAPI.Models;

public class Category : IAuditEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}