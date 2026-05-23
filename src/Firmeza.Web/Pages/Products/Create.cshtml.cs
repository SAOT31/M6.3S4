using Firmeza.Infrastructure.Data;
using Firmeza.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Firmeza.Web.Pages.Products;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ProductViewModel Product { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Valida campos del formulario
        if (!ModelState.IsValid)
            return Page();

        try
        {
            // Mapea ViewModel a Entidad de dominio
            var product = new Core.Entities.Product
            {
                Name        = Product.Name,
                Description = Product.Description,
                Category    = Product.Category,
                Unit        = Product.Unit,
                Price       = Product.Price,
                Stock       = Product.Stock,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            _context.Products.Add(product);

            // Guarda los cambios en BD
            await _context.SaveChangesAsync();

            // Redirección al listado
            return RedirectToPage("/Products/Index");
        }
        catch (Exception ex)
        {
            // Captura errores de persistencia
            ErrorMessage = $"Error saving product: {ex.Message}";
            return Page();
        }
    }
}
