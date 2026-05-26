using System.Text;
using Firmeza.Core.Entities;
using Firmeza.Core.Enums;
using Firmeza.Core.Interfaces;
using Firmeza.Infrastructure.Data;
using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? "SuperSecretKeyThatIsAtLeast32CharactersLongAndSecure123!";
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "FirmezaApi",
        ValidAudience = jwtSettings["Audience"] ?? "FirmezaClient",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("ClienteOnly", policy => policy.RequireRole(AppRoles.Cliente));
    options.AddPolicy("AdminOrCliente", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Cliente));
});

// Services
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<Firmeza.Api.Services.PdfReceiptService>();
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Firmeza API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer' seguido de un espacio y el token JWT.\nEjemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed database
await SeedDatabase(app);

if (app.Environment.IsDevelopment() || true) // Permitimos Swagger en cualquier ambiente para pruebas
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Firmeza API v1"));
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        string[] roles = [AppRoles.Admin, AppRoles.Customer, AppRoles.Cliente];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "admin@firmeza.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Administrador",
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
        logger.LogError(ex, "Error en la inicialización de la base de datos");
    }
}
