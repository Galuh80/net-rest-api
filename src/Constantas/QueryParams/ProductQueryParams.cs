namespace RestAPI.Constantas.QueryParams;

public class ProductQueryParams : BaseQueryParams
{
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
