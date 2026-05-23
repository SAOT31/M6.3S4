using Firmeza.Infrastructure.Data;
using Firmeza.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Firmeza.Web.Pages.Clients;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ClientViewModel Client { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? AgeError     { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);

        if (client is null)
            return NotFound();

        Client = new ClientViewModel
        {
            Id             = client.Id,
            FirstName      = client.FirstName,
            LastName       = client.LastName,
            DocumentType   = client.DocumentType,
            DocumentNumber = client.DocumentNumber,
            Email          = client.Email,
            Phone          = client.Phone,
            Address        = client.Address,
            AgeInput       = client.Age.ToString()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Convierte y valida la edad
        int parsedAge;
        try
        {
            parsedAge = int.Parse(Client.AgeInput);
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

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var client = await _context.Clients.FindAsync(Client.Id);
            if (client is null) return NotFound();

            client.FirstName      = Client.FirstName;
            client.LastName       = Client.LastName;
            client.DocumentType   = Client.DocumentType;
            client.DocumentNumber = Client.DocumentNumber;
            client.Email          = Client.Email;
            client.Phone          = Client.Phone;
            client.Address        = Client.Address;
            client.Age            = parsedAge;

            await _context.SaveChangesAsync();
            return RedirectToPage("/Clients/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating client: {ex.Message}";
            return Page();
        }
    }
}
