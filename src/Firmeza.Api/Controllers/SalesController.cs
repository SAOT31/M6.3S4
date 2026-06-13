using System.Security.Claims;
using AutoMapper;
using Firmeza.Api.Dtos;
using Firmeza.Api.Services;
using Firmeza.Core.Entities;
using Firmeza.Core.Interfaces;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly PdfReceiptService _pdfService;

    public SalesController(ApplicationDbContext context, IMapper mapper, IEmailService emailService, PdfReceiptService pdfService)
    {
        _context = context;
        _mapper = mapper;
        _emailService = emailService;
        _pdfService = pdfService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetSales()
    {
        var sales = await _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details)
                .ThenInclude(sd => sd.Product)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<SaleDto>>(sales));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SaleDto>> GetSale(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details)
                .ThenInclude(sd => sd.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null)
            return NotFound("Venta no encontrada.");

        return Ok(_mapper.Map<SaleDto>(sale));
    }

    [HttpGet("{id}/receipt")]
    public async Task<IActionResult> GetReceipt(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details)
                .ThenInclude(sd => sd.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null)
            return NotFound("Venta no encontrada.");

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

        if (userRole != "Admin" && sale.Client?.Email != userEmail)
        {
            return Forbid();
        }

        var pdfBytes = _pdfService.GenerateReceiptBytes(sale);
        return File(pdfBytes, "application/pdf", $"comprobante-{sale.Id:D6}.pdf");
    }

    [HttpPost]
    public async Task<ActionResult<SaleDto>> CreateSale([FromBody] CreateSaleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var client = await _context.Clients.FindAsync(dto.ClientId);
        if (client == null)
            return BadRequest("El cliente especificado no existe.");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                     User.FindFirst("sub")?.Value ?? 
                     "API";

        var sale = new Sale
        {
            ClientId = client.Id,
            Client = client,
            UserId = userId,
            SaleDate = DateTime.UtcNow,
            Status = "Completed",
            Total = 0
        };

        decimal total = 0;

        foreach (var detailDto in dto.Details)
        {
            var product = await _context.Products.FindAsync(detailDto.ProductId);
            if (product == null)
                return BadRequest($"El producto con ID {detailDto.ProductId} no existe.");

            if (product.Stock < detailDto.Quantity)
                return BadRequest($"Stock insuficiente para el producto: {product.Name}. Stock disponible: {product.Stock}");

            product.Stock -= detailDto.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var detail = new SaleDetail
            {
                Product = product,
                ProductId = product.Id,
                Quantity = detailDto.Quantity,
                UnitPrice = product.Price
            };

            sale.Details.Add(detail);
            total += detail.Subtotal;
        }

        sale.Total = total;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        // Enviar correo de confirmación de compra con resumen y PDF adjunto
        try
        {
            var fullSale = await _context.Sales
                .Include(s => s.Client)
                .Include(s => s.Details)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (fullSale != null)
            {
                var pdfBytes = _pdfService.GenerateReceiptBytes(fullSale);

                var detailLines = string.Join("", fullSale.Details.Select(d => 
                    $"<li>{(d.Product != null ? d.Product.Name : "Producto")} x{d.Quantity} - Subtotal: ${d.Subtotal}</li>"));

                var emailBody = $@"
                    <h1>Purchase Confirmation - Firmeza</h1>
                    <p>Hello {client.FullName},</p>
                    <p>Thank you for your purchase. Please find the official receipt attached to this email in PDF format.</p>
                    <p>Order summary:</p>
                    <ul>
                        {detailLines}
                    </ul>
                    <h3>Total Paid: ${fullSale.Total}</h3>
                    <p>Date: {fullSale.SaleDate:MM/dd/yyyy HH:mm}</p>
                ";

                await _emailService.SendEmailWithAttachmentAsync(
                    client.Email,
                    $"Purchase Confirmation #{fullSale.Id} - Firmeza",
                    emailBody,
                    pdfBytes,
                    $"receipt-{fullSale.Id:D6}.pdf"
                );
            }
        }
        catch
        {
            // Ignoramos fallos de correo para no revertir la transacción en BD
        }

        var resultDto = _mapper.Map<SaleDto>(sale);
        return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, resultDto);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteSale(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null)
            return NotFound("Venta no encontrada.");

        // Devolver stock al eliminar la venta
        foreach (var detail in sale.Details)
        {
            var product = await _context.Products.FindAsync(detail.ProductId);
            if (product != null)
            {
                product.Stock += detail.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
