using Firmeza.Core.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Pages.Sales;

[Authorize(Roles = "Admin")]
public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailModel(ApplicationDbContext context) => _context = context;

    public Sale? Sale { get; set; }

    public async Task OnGetAsync(int id)
    {
        Sale = await _context.Sales
            .Include(s => s.Client)
            .Include(s => s.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
