using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Dtos;

public class SaleDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SaleDetailDto> Details { get; set; } = new();
}

public class SaleDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateSaleDto
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "Los detalles de la venta son obligatorios.")]
    [MinLength(1, ErrorMessage = "Debe agregar al menos un producto a la venta.")]
    public List<CreateSaleDetailDto> Details { get; set; } = new();
}

public class CreateSaleDetailDto
{
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
    public int Quantity { get; set; }
}
