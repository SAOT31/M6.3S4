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
        logger.LogError(ex, "Error en la inicialización de la base de datos");
    }
}
