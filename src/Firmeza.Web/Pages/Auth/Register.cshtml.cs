using Firmeza.Core.Enums;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser>  _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(UserManager<ApplicationUser>  userManager,
                         SignInManager<ApplicationUser> signInManager)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            // Crea instancia del usuario
            var user = new ApplicationUser
            {
                UserName       = Input.Email,
                Email          = Input.Email,
                DisplayName    = $"{Input.FirstName} {Input.LastName}",
                EmailConfirmed = true
            };

            // Registra el usuario con la contraseña hasheada
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                // Recopila errores devueltos por Identity
                ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                return Page();
            }

            // Asigna rol de cliente por defecto
            await _userManager.AddToRoleAsync(user, AppRoles.Customer);

            // Guarda mensaje de éxito para la redirección
            TempData["SuccessMessage"] = "Account created successfully. Please sign in.";
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration error: {ex.Message}";
            return Page();
        }
    }
}

public class RegisterInput
{
    [Required(ErrorMessage = "First name is required")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    // Regla de longitud para contraseña
    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    // Compara con el campo contraseña
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
