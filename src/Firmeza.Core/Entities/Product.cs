namespace Firmeza.Core.Entities;

public class Product
{
    // EF Core usa Id como clave primaria por convención (no necesita atributo [Key])
    public int Id { get; set; }

    // Nombre del producto, ej: "Cemento Gris 50kg"
    public string Name { get; set; } = string.Empty;

    // Descripción opcional — para mostrar detalles adicionales en la ficha del producto
    public string Description { get; set; } = string.Empty;

    // Categoría para agrupar y filtrar: Cemento, Varilla, Pintura, Arena, etc.
    public string Category { get; set; } = string.Empty;

    // Unidad de medida en la que se vende: kg, m², bolsa, unidad, litro
    public string Unit { get; set; } = string.Empty;

    // Precio unitario de venta
    public decimal Price { get; set; }

    // Unidades disponibles en bodega — si cae por debajo de 10 aparece la alerta en el dashboard
    public int Stock { get; set; }

    // Fecha de registro del producto en el sistema
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Se actualiza cada vez que se edita el producto
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Propiedad de navegación — EF la usa para hacer los JOINs con SaleDetail
    // ICollection porque un producto puede aparecer en muchos detalles de venta
    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
}
