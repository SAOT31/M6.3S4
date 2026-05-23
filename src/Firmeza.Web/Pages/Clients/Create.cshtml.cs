using Firmeza.Infrastructure.Data;
using Firmeza.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Firmeza.Web.Pages.Clients;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ClientViewModel Client { get; set; } = new();

    public string? ErrorMessage { get; set; }

    // Mensaje de error específico para el campo edad
    public string? AgeError { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        int parsedAge;
        try
        {
            // Convierte la edad recibida en texto a entero
            parsedAge = int.Parse(Client.AgeInput);

            // Valida rango de edad
            if (parsedAge < 0 || parsedAge > 120)
                throw new ArgumentOutOfRangeException(nameof(parsedAge), "Age must be between 0 and 120.");
        }
        catch (FormatException)
        {
            AgeError = "Age must be a whole number (e.g. 25).";
            return Page();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            AgeError = ex.Message;
            return Page();
        }

        // Valida otras reglas del modelo
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var client = new Core.Entities.Client
            {
                FirstName      = Client.FirstName,
                LastName       = Client.LastName,
                DocumentType   = Client.DocumentType,
                DocumentNumber = Client.DocumentNumber,
                Email          = Client.Email,
                Phone          = Client.Phone,
                Address        = Client.Address,
                Age            = parsedAge,
                CreatedAt      = DateTime.UtcNow
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Clients/Index");
        }
        catch (Exception ex)
        {
            // Controla errores de persistencia (ej. llaves duplicadas)
            ErrorMessage = $"Error saving client: {ex.Message}";
            return Page();
        }
    }
}
