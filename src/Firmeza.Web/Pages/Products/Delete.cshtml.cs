using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Firmeza.Web.Pages.Products;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int     ProductId   { get; set; }
    public string? ProductName { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product is null)
            return NotFound();

        ProductId   = product.Id;
        ProductName = product.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Products/Index");
        }
        catch (Exception ex)
        {
            // Error si está referenciado en otra tabla
            ErrorMessage = $"Cannot delete this product: {ex.Message}";
            ProductId    = id;

            var p = await _context.Products.FindAsync(id);
            if (p != null) ProductName = p.Name;

            return Page();
        }
    }
}
