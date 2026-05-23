using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Firmeza.Web.Pages.Clients;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int     ClientId    { get; set; }
    public string? ClientName  { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client is null) return NotFound();

        ClientId   = client.Id;
        ClientName = $"{client.FirstName} {client.LastName}";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            var client = await _context.Clients.FindAsync(id);
            if (client is null) return NotFound();

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Clients/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Cannot delete this client: {ex.Message}";
            ClientId = id;

            var c = await _context.Clients.FindAsync(id);
            if (c != null) ClientName = $"{c.FirstName} {c.LastName}";

            return Page();
        }
    }
}
