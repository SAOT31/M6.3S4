using Firmeza.Core.Enums;
using Firmeza.Core.Services;
using Firmeza.Infrastructure.Data;
using Firmeza.Infrastructure.Identity;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Infrastructure;

// Licencias de librerías de exportación
ExcelPackage.License.SetNonCommercialOrganization("Firmeza");
QuestPDF.Settings.License   = LicenseType.Community;

// Configuración inicial de la aplicación
var builder = WebApplication.CreateBuilder(args);

// Base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Reglas de contraseña
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase       = false;

    // Registro simple sin confirmar email
    options.SignIn.RequireConfirmedAccount = false;
})
// Almacén de base de datos
.AddEntityFrameworkStores<ApplicationDbContext>()
// Proveedores de tokens por defecto
.AddDefaultTokenProviders();

// Configuración de cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan   = TimeSpan.FromHours(8);
});

// Soporte para Razor Pages
builder.Services.AddRazorPages();

// Servicios de dominio
builder.Services.AddScoped<ImportParserService>();
builder.Services.AddScoped<PdfReceiptService>();

// Construcción de la aplicación
var app = builder.Build();

// Inicialización de datos
await SeedDatabase(app);

// Canal de procesamiento (Middlewares)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Redirección al Dashboard
app.MapGet("/", () => Results.Redirect("/Dashboard"));

app.Run();

// Inicializa roles y usuario administrador
static async Task SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context     = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ejecuta migraciones pendientes
        await context.Database.MigrateAsync();

        // Creación de roles si no existen
        string[] roles = [AppRoles.Admin, AppRoles.Customer, AppRoles.Cliente];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Creación de administrador por defecto
        const string adminEmail = "admin@firmeza.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName       = adminEmail,
                Email          = adminEmail,
                DisplayName    = "Administrador",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la inicialización de la base de datos");
    }
}

