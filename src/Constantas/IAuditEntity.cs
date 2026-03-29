namespace RestAPI.Constantas;

public interface IAuditEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}