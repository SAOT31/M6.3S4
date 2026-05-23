namespace Firmeza.Core.Entities;

public class Sale
{
    public int Id { get; set; }

    // FK hacia la tabla de clientes — el cliente que hizo la compra
    public int ClientId { get; set; }

    // Propiedad de navegación — EF la llena con Include(s => s.Client)
    // null! le dice al compilador que EF se encarga de poblarlo, no tú
    public Client Client { get; set; } = null!;

    // Id del usuario (admin) que registró la venta — viene de AspNetUsers
    public string UserId { get; set; } = string.Empty;

    // Fecha y hora exacta en que se creó la venta
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    // Total calculado sumando todos los subtotales de los detalles
    public decimal Total { get; set; }

    // Estado del pedido: Pending (pendiente), Completed (completado), Cancelled (cancelado)
    public string Status { get; set; } = "Pending";

    // Lista de productos dentro de esta venta
    // Una venta puede tener varios productos → ICollection
    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}
