using Firmeza.Infrastructure.Data;
using Firmeza.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Pages.Dashboard;

// Autorización restringida a administradores
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    // Contexto de base de datos
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // Modelo de datos para la vista
    public DashboardViewModel Stats { get; set; } = new();

    // Consulta estadísticas del dashboard
    public async Task OnGetAsync()
    {
        // Cuenta totales
        Stats.TotalProducts = await _context.Products.CountAsync();
        Stats.TotalClients  = await _context.Clients.CountAsync();
        Stats.TotalSales    = await _context.Sales.CountAsync();

        // Suma ingresos de ventas completadas
        Stats.TotalRevenue = await _context.Sales
            .Where(s => s.Status == "Completed")
            .SumAsync(s => (decimal?)s.Total) ?? 0;

        // Consulta las últimas 5 ventas
        Stats.RecentSales = await _context.Sales
            .Include(s => s.Client)
            .OrderByDescending(s => s.SaleDate)
            .Take(5)
            .Select(s => new RecentSaleItem
            {
                SaleId     = s.Id,
                ClientName = s.Client.FirstName + " " + s.Client.LastName,
                Total      = s.Total,
                Status     = s.Status,
                Date       = s.SaleDate
            })
            .ToListAsync();

        // Consulta productos con bajo stock (< 10)
        Stats.LowStockProducts = await _context.Products
            .Where(p => p.Stock < 10)
            .OrderBy(p => p.Stock)
            .Select(p => new LowStockItem
            {
                ProductName = p.Name,
                Stock       = p.Stock,
                Unit        = p.Unit
            })
            .ToListAsync();
    }
}
