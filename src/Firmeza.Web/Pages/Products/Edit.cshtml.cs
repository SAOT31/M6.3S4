using Firmeza.Infrastructure.Data;
using Firmeza.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Firmeza.Web.Pages.Products;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ProductViewModel Product { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product is null)
            return NotFound();

        // Llena el ViewModel
        Product = new ProductViewModel
        {
            Id          = product.Id,
            Name        = product.Name,
            Description = product.Description,
            Category    = product.Category,
            Unit        = product.Unit,
            Price       = product.Price,
            Stock       = product.Stock
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var product = await _context.Products.FindAsync(Product.Id);

            if (product is null)
                return NotFound();

            product.Name        = Product.Name;
            product.Description = Product.Description;
            product.Category    = Product.Category;
            product.Unit        = Product.Unit;
            product.Price       = Product.Price;
            product.Stock       = Product.Stock;
            product.UpdatedAt   = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Products/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating product: {ex.Message}";
            return Page();
        }
    }
}
