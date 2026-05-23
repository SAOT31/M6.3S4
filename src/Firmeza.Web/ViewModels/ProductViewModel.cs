using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.ViewModels;

public class ProductViewModel
{
    // Identificador único (usado en edición)
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(150, ErrorMessage = "Max 150 characters")]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required")]
    [Display(Name = "Unit")]
    public string Unit { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 99999999, ErrorMessage = "Price must be greater than 0")]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    [Display(Name = "Stock")]
    public int Stock { get; set; }

    // Categorías y unidades para los formularios
    public static List<string> Categories =>
        ["Cement", "Steel Bar", "Paint", "Sand", "Brick", "Tile", "Other"];

    public static List<string> Units =>
        ["unit", "kg", "m²", "m³", "bag", "liter", "box"];
}
