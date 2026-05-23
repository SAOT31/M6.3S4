using System.Text.Json;
using Firmeza.Core.Entities;
using Firmeza.Infrastructure.Data;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Pages.Sales;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PdfReceiptService    _pdf;
    private readonly UserManager<Infrastructure.Identity.ApplicationUser> _userManager;

    public CreateModel(
        ApplicationDbContext context,
        PdfReceiptService pdf,
        UserManager<Infrastructure.Identity.ApplicationUser> userManager)
    {
        _context     = context;
        _pdf         = pdf;
        _userManager = userManager;
    }

    // Datos para los selects
    public List<Client>  Clients  { get; set; } = [];
    public List<Product> Products { get; set; } = [];

    [BindProperty] public int    ClientId  { get; set; }
    [BindProperty] public string CartJson  { get; set; } = "[]";

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadSelectsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadSelectsAsync();

        // Deserializar carrito
        List<CartItem>? cartItems;
        try
        {
            cartItems = JsonSerializer.Deserialize<List<CartItem>>(CartJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            ErrorMessage = "Invalid cart data.";
            return Page();
        }

        if (cartItems is null || cartItems.Count == 0)
        {
            ErrorMessage = "Please add at least one product.";
            return Page();
        }

        if (ClientId == 0)
        {
            ErrorMessage = "Please select a client.";
            return Page();
        }

        // Cargar productos de la BD para obtener precio real
        var productIds    = cartItems.Select(c => c.ProductId).Distinct().ToList();
        var dbProducts    = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Validar stock suficiente
        foreach (var item in cartItems)
        {
            if (!dbProducts.TryGetValue(item.ProductId, out var prod))
            {
                ErrorMessage = $"Product ID {item.ProductId} not found.";
                return Page();
            }
            if (prod.Stock < item.Qty)
            {
                ErrorMessage = $"Insufficient stock for '{prod.Name}'. Available: {prod.Stock}.";
                return Page();
            }
        }

        // Construir la venta
        var userId = _userManager.GetUserId(User)!;
        var sale = new Sale
        {
            ClientId = ClientId,
            UserId   = userId,
            SaleDate = DateTime.UtcNow,
            Status   = "Completed",
        };

        var details = cartItems.Select(item =>
        {
            var prod = dbProducts[item.ProductId];
            return new SaleDetail
            {
                ProductId = item.ProductId,
                Quantity  = item.Qty,
                UnitPrice = prod.Price,
            };
        }).ToList();

        sale.Total   = details.Sum(d => d.Quantity * d.UnitPrice);
        sale.Details = details;

        // Descontar stock
        foreach (var item in cartItems)
            dbProducts[item.ProductId].Stock -= item.Qty;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        // Generar PDF (recargar con navegación completa)
        var fullSale = await _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details).ThenInclude(d => d.Product)
            .FirstAsync(s => s.Id == sale.Id);

        _pdf.GenerateReceipt(fullSale);

        TempData["Success"] = $"Sale #{sale.Id} registered. Receipt PDF generated.";
        return RedirectToPage("/Sales/Index");
    }

    private async Task LoadSelectsAsync()
    {
        Clients  = await _context.Clients.OrderBy(c => c.FirstName).ToListAsync();
        Products = await _context.Products.Where(p => p.Stock > 0).OrderBy(p => p.Name).ToListAsync();
    }

    // DTO interno para deserializar el carrito
    private record CartItem(int ProductId, int Qty);
}
