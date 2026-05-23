using Firmeza.Core.Entities;

namespace Firmeza.Tests.Core;

public class SaleDetailTests
{
    [Fact]
    public void Subtotal_EsCorrecto_CuandoHayCantidadYPrecio()
    {
        var detalle = new SaleDetail { Quantity = 3, UnitPrice = 25.50m };
        Assert.Equal(76.50m, detalle.Subtotal);
    }

    [Theory]
    [InlineData(1, 100.0, 100.0)]
    [InlineData(5, 20.0,  100.0)]
    [InlineData(10, 0.0,    0.0)]
    [InlineData(2, 15.75,  31.50)]
    public void Subtotal_EsProductoDeCantidadPorPrecio(int cantidad, double precio, double esperado)
    {
        var detalle = new SaleDetail
        {
            Quantity  = cantidad,
            UnitPrice = (decimal)precio,
        };
        Assert.Equal((decimal)esperado, detalle.Subtotal);
    }

    [Fact]
    public void Subtotal_EsCero_CuandoCantidadEsCero()
    {
        var detalle = new SaleDetail { Quantity = 0, UnitPrice = 99.99m };
        Assert.Equal(0m, detalle.Subtotal);
    }
}
