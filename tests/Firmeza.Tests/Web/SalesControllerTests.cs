using System.Security.Claims;
using AutoMapper;
using Firmeza.Api.Controllers;
using Firmeza.Api.Mappings;
using Firmeza.Api.Services;
using Firmeza.Core.Entities;
using Firmeza.Core.Interfaces;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Tests.Web;

public class FakeEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
    public Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachmentBytes, string attachmentName) => Task.CompletedTask;
}

public class SalesControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly PdfReceiptService _pdfService;

    public SalesControllerTests()
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
        _emailService = new FakeEmailService();
        _pdfService = new PdfReceiptService();

        // Inicializar la licencia de QuestPDF para la prueba
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    [Fact]
    public async Task GetReceipt_ReturnsPdfFile_ForAdmin()
    {
        // Arrange
        var client = new Client { Id = 1, FirstName = "Carlos", LastName = "Gomez", Email = "carlos@correo.com" };
        var product = new Product { Id = 1, Name = "Cemento", Price = 10m, Stock = 100 };
        _context.Clients.Add(client);
        _context.Products.Add(product);

        var sale = new Sale { Id = 1, ClientId = 1, Client = client, Total = 50m, SaleDate = DateTime.UtcNow, Status = "Completed" };
        sale.Details.Add(new SaleDetail { Id = 1, SaleId = 1, ProductId = 1, Product = product, Quantity = 5, UnitPrice = 10m });
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Email, "admin@firmeza.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new SalesController(_context, _mapper, _emailService, _pdfService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        // Act
        var result = await controller.GetReceipt(1);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("comprobante-000001.pdf", fileResult.FileDownloadName);
        Assert.NotEmpty(fileResult.FileContents);
    }

    [Fact]
    public async Task GetReceipt_ReturnsForbid_ForNonAdmin_WithDifferentEmail()
    {
        // Arrange
        var client = new Client { Id = 1, FirstName = "Carlos", LastName = "Gomez", Email = "carlos@correo.com" };
        _context.Clients.Add(client);
        var sale = new Sale { Id = 1, ClientId = 1, Client = client, Total = 0 };
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Cliente"),
            new Claim(ClaimTypes.Email, "otro@correo.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new SalesController(_context, _mapper, _emailService, _pdfService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        // Act
        var result = await controller.GetReceipt(1);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
