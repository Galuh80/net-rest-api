using System.ComponentModel.DataAnnotations;

namespace RestAPI.Constantas.DTOs.Product;

public class CreateProductDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "CategoryId is required")]
    public Guid CategoryId { get; set; }

    public IFormFile? Image { get; set; }
}
