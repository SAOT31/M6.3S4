using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProductDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

public class UpdateProductDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}
