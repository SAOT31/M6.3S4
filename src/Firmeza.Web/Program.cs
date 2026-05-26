using Firmeza.Core.Entities;
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

        // Seeding de productos por defecto (Materiales y Vehículos)
        if (!await context.Products.AnyAsync())
        {
            var defaultProducts = new List<Product>
            {
                new Product { Name = "Cemento Gris Especial 50kg", Description = "Cemento gris de alta resistencia para todo tipo de obras y mezclas de concreto.", Category = "Materiales", Unit = "Bolsa", Price = 28500m, Stock = 150 },
                new Product { Name = "Varilla Corrugada 1/2 pulgada", Description = "Varilla de acero de 1/2 pulgada de diámetro, longitud de 6 metros para refuerzo estructural.", Category = "Materiales", Unit = "Unidad", Price = 32000m, Stock = 200 },
                new Product { Name = "Ladrillo Limpio Arcilla", Description = "Ladrillo de arcilla cocida para muros estructurales y divisiones.", Category = "Materiales", Unit = "Unidad", Price = 1200m, Stock = 1000 },
                new Product { Name = "Arena de Río Lavada", Description = "Arena fina lavada ideal para pañete, mortero y acabados.", Category = "Materiales", Unit = "M3", Price = 65000m, Stock = 80 },
                new Product { Name = "Pintura Acrílica Exterior Blanco Galón", Description = "Pintura vinilo acrílica tipo 1 de alta durabilidad y resistencia a la intemperie.", Category = "Materiales", Unit = "Galón", Price = 48000m, Stock = 45 },
                
                new Product { Name = "Camión Volqueta Dobletroque", Description = "Alquiler de camión volqueta dobletroque de 15m³ de capacidad con operario calificado.", Category = "Vehículos", Unit = "Día", Price = 450000m, Stock = 5 },
                new Product { Name = "Mezcladora de Concreto 1 Bulto", Description = "Alquiler de trompo mezclador de concreto con motor a gasolina de 9HP.", Category = "Vehículos", Unit = "Día", Price = 85000m, Stock = 12 },
                new Product { Name = "Miniexcavadora Orugas", Description = "Alquiler de miniexcavadora con balde de 0.1m³ y orugas de goma, ideal para excavaciones en espacios reducidos.", Category = "Vehículos", Unit = "Día", Price = 600000m, Stock = 3 },
                new Product { Name = "Camión Mixer Hormigonera", Description = "Alquiler de camión mezclador de concreto de 8m³ con conductor y operario de bomba.", Category = "Vehículos", Unit = "Día", Price = 950000m, Stock = 2 }
            };
            await context.Products.AddRangeAsync(defaultProducts);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la inicialización de la base de datos");
    }
}

