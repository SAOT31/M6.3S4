using Firmeza.Core.Enums;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Pages.Auth;

public class LoginModel : PageModel
{
    // Manejo de autenticación y consulta de usuarios
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser>  _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager,
                      UserManager<ApplicationUser>  userManager)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Intenta iniciar sesión
        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        // Restricción de acceso para el rol Customer
        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user != null && await _userManager.IsInRoleAsync(user, AppRoles.Customer))
        {
            await _signInManager.SignOutAsync();
            ErrorMessage = "Access denied. This panel is for administrators only.";
            return Page();
        }

        return RedirectToPage("/Dashboard/Index");
    }
}

// Modelo para el formulario de login
public class LoginInput
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
