using Firmeza.Core.Entities;
using Xunit;

namespace Firmeza.Tests.Web;

// Pruebas unitarias de modelos
public class CreateProductModelTests
{
    [Fact]
    public void SaleDetail_Subtotal_ShouldBeQuantityTimesUnitPrice()
    {
        var detail = new SaleDetail
        {
            Quantity  = 3,
            UnitPrice = 25.50m
        };

        Assert.Equal(76.50m, detail.Subtotal);
    }

    [Fact]
    public void Client_FullName_ShouldCombineFirstAndLastName()
    {
        var client = new Client
        {
            FirstName = "Juan",
            LastName  = "Pérez"
        };

        Assert.Equal("Juan Pérez", client.FullName);
    }

    [Fact]
    public void Client_Sales_ShouldInitializeEmpty()
    {
        var client = new Client();
        Assert.NotNull(client.Sales);
        Assert.Empty(client.Sales);
    }
}
