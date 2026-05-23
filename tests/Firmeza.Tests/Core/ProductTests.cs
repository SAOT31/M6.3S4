using Firmeza.Core.Entities;
using Xunit;

namespace Firmeza.Tests.Core;

// Pruebas unitarias de la entidad Product
public class ProductTests
{
    [Fact]
    public void Product_DefaultStock_ShouldBeZero()
    {
        var product = new Product();
        Assert.Equal(0, product.Stock);
    }

    [Fact]
    public void Product_DefaultPrice_ShouldBeZero()
    {
        var product = new Product();
        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void Product_Name_ShouldNotBeEmpty_AfterAssignment()
    {
        var product = new Product { Name = "Gray Cement" };
        Assert.False(string.IsNullOrEmpty(product.Name));
    }

    [Fact]
    public void Product_SaleDetails_ShouldInitializeEmpty()
    {
        var product = new Product();
        Assert.NotNull(product.SaleDetails);
        Assert.Empty(product.SaleDetails);
    }

    [Theory]
    [InlineData(0,   true)]
    [InlineData(9,   true)]
    [InlineData(10,  false)]
    [InlineData(100, false)]
    public void Product_LowStock_DetectedCorrectly(int stock, bool expectedLow)
    {
        var product = new Product { Stock = stock };
        bool isLow  = product.Stock < 10;
        Assert.Equal(expectedLow, isLow);
    }
}
