using Microsoft.AspNetCore.Identity;

namespace Firmeza.Infrastructure.Identity;

// IdentityUser ya tiene: Id, UserName, Email, PasswordHash, PhoneNumber,
// LockoutEnabled, TwoFactorEnabled, AccessFailedCount, etc.
// Al heredar de él, obtenemos todo eso gratis y solo agregamos lo que necesitamos
public class ApplicationUser : IdentityUser
{
    // Campo extra para mostrar el nombre en el sidebar del panel
    // No lo trae IdentityUser por defecto, lo añadimos nosotros
    public string DisplayName { get; set; } = string.Empty;
}
