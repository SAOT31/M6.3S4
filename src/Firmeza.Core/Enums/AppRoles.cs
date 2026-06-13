namespace Firmeza.Core.Enums;

// Identity maneja los roles como strings, no como enum
// Si usáramos enum tendríamos que convertirlo a string cada vez que llamamos
// a roleManager.CreateAsync() o userManager.AddToRoleAsync()
// Con constantes string se usa directamente: AppRoles.Admin
public static class AppRoles
{
    public const string Admin    = "Admin";
    public const string Cliente  = "Cliente";
    // Customer es un alias de Cliente para mantener compatibilidad con el panel Web MVC
    public const string Customer = Cliente;
}
