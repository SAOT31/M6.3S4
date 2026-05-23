using Firmeza.Core.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Pages.Products;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lista de productos para la tabla
    public List<Product> Products { get; set; } = [];

    // Parámetros de búsqueda en la UI
    public string? Search   { get; set; }
    public string? Category { get; set; }

    public async Task OnGetAsync(string? search, string? category)
    {
        Search   = search;
        Category = category;

        // Construcción condicional de la consulta
        var query = _context.Products.AsQueryable();

        // Filtra por nombre
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        // Filtra por categoría
        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        // Ejecuta la consulta ordenada
        Products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
