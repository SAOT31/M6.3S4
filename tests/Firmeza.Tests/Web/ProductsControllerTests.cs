using AutoMapper;
using Firmeza.Api.Controllers;
using Firmeza.Api.Dtos;
using Firmeza.Api.Mappings;
using Firmeza.Core.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Tests.Web;

public class ProductsControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ProductsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        _context.Products.Add(new Product { Id = 1, Name = "Cemento", Category = "Construccion" });
        _context.Products.Add(new Product { Id = 2, Name = "Arena", Category = "Construccion" });
        await _context.SaveChangesAsync();

        var controller = new ProductsController(_context, _mapper);

        var result = await controller.GetProducts(null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IEnumerable<ProductDto>>(okResult.Value);
        Assert.Equal(2, products.Count());
    }

    [Fact]
    public async Task CreateProduct_AddsProductAndReturnsCreated()
    {
        var controller = new ProductsController(_context, _mapper);
        var createDto = new CreateProductDto
        {
            Name = "Varilla",
            Description = "Varilla de fierro",
            Category = "Fierros",
            Unit = "Unidad",
            Price = 15.5m,
            Stock = 100
        };

        var result = await controller.CreateProduct(createDto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedDto = Assert.IsType<ProductDto>(createdResult.Value);
        Assert.Equal("Varilla", returnedDto.Name);
        Assert.True(await _context.Products.AnyAsync(p => p.Name == "Varilla"));
    }

    [Fact]
    public async Task DeleteProduct_ReturnsBadRequest_IfProductInSales()
    {
        var product = new Product { Id = 1, Name = "Cemento", Category = "Construccion" };
        _context.Products.Add(product);

        var saleDetail = new SaleDetail
        {
            Id = 1,
            ProductId = 1,
            Product = product,
            Quantity = 5,
            UnitPrice = 10m
        };
        _context.SaleDetails.Add(saleDetail);
        await _context.SaveChangesAsync();

        var controller = new ProductsController(_context, _mapper);

        var result = await controller.DeleteProduct(1);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No se puede eliminar el producto porque está asociado a ventas existentes.", badRequestResult.Value);
    }
}
