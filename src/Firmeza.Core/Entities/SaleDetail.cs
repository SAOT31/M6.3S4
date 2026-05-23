namespace Firmeza.Core.Entities;

// SaleDetail es la tabla puente entre Sale y Product
// Cada fila representa UN producto dentro de UNA venta
public class SaleDetail
{
    public int Id { get; set; }

    // FK hacia la venta a la que pertenece este detalle
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    // FK hacia el producto que se vendió
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Cuántas unidades se vendieron de este producto en esta venta
    public int Quantity { get; set; }

    // Precio al momento de la venta — se guarda para que no cambie
    // si después se edita el precio del producto
    public decimal UnitPrice { get; set; }

    // Propiedad calculada: no va a la BD, se recalcula en memoria
    // => es una "expression-bodied property" — equivale a { get { return Quantity * UnitPrice; } }
    public decimal Subtotal => Quantity * UnitPrice;
}
