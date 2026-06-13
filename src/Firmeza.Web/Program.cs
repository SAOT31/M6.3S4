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
                DisplayName    = "Administrator",
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
                new Product { Name = "Special Gray Cement 50kg", Description = "High-strength gray cement for all types of construction work and concrete mixes.", Category = "Materials", Unit = "Bag", Price = 28500m, Stock = 150 },
                new Product { Name = "Corrugated Steel Bar 1/2 inch", Description = "1/2 inch diameter steel rebar, 6 meters long, for structural reinforcement.", Category = "Materials", Unit = "Unit", Price = 32000m, Stock = 200 },
                new Product { Name = "Clean Clay Brick", Description = "Fired clay brick for structural walls and partitions.", Category = "Materials", Unit = "Unit", Price = 1200m, Stock = 1000 },
                new Product { Name = "Washed River Sand", Description = "Fine washed river sand, ideal for plastering, mortar and finishing.", Category = "Materials", Unit = "M3", Price = 65000m, Stock = 80 },
                new Product { Name = "Exterior Acrylic Paint White Gallon", Description = "Type 1 acrylic vinyl paint, high durability and weather resistance.", Category = "Materials", Unit = "Gallon", Price = 48000m, Stock = 45 },
                
                new Product { Name = "Double-axle Dump Truck", Description = "Rental of 15m³ double-axle dump truck with qualified operator.", Category = "Vehicles", Unit = "Day", Price = 450000m, Stock = 5 },
                new Product { Name = "Concrete Mixer 1-Bag", Description = "Rental of concrete drum mixer with 9HP gasoline engine.", Category = "Vehicles", Unit = "Day", Price = 85000m, Stock = 12 },
                new Product { Name = "Mini Crawler Excavator", Description = "Rental of mini excavator with 0.1m³ bucket and rubber tracks, ideal for excavation in tight spaces.", Category = "Vehicles", Unit = "Day", Price = 600000m, Stock = 3 },
                new Product { Name = "Concrete Mixer Truck", Description = "Rental of 8m³ concrete mixer truck with driver and pump operator.", Category = "Vehicles", Unit = "Day", Price = 950000m, Stock = 2 }
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

