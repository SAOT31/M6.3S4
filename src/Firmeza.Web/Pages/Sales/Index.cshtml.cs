using Firmeza.Core.Entities;
using Firmeza.Infrastructure.Data;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Pages.Sales;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Sale> Sales { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }

    public async Task OnGetAsync(string? search, string? status)
    {
        Search = search;
        Status = status;

        var query = _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var lower = search.ToLower();
            query = query.Where(s =>
                (s.Client.FirstName + " " + s.Client.LastName).ToLower().Contains(lower));
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        Sales = await query
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetDownloadReceiptAsync(int id, [FromServices] PdfReceiptService pdfService)
    {
        var sale = await _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale is null)
            return NotFound();

        var bytes = pdfService.GenerateReceiptBytes(sale);
        return File(bytes, "application/pdf", $"recibo-{sale.Id}.pdf");
    }
}

