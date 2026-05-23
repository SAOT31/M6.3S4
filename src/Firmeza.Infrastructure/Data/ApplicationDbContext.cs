using Firmeza.Core.Entities;
using Firmeza.Core.Interfaces;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Data;

// IdentityDbContext ya incluye las tablas de Identity (AspNetUsers, AspNetRoles, etc.)
// Al heredar de él también implementamos IApplicationDbContext (nuestro contrato propio)
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    // El constructor recibe las opciones de conexión que se configuran en Program.cs
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // DbSet representa cada tabla en la base de datos
    // Al hacer _context.Products ya tienes acceso a la tabla de productos
    public DbSet<Product>    Products    => Set<Product>();
    public DbSet<Client>     Clients     => Set<Client>();
    public DbSet<Sale>       Sales       => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    // OnModelCreating se ejecuta cuando EF construye el modelo — aquí van las reglas
    // que no se pueden expresar solo con atributos en las entidades
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Siempre llama al base primero para que Identity configure sus tablas
        base.OnModelCreating(builder);

        // Un cliente no puede tener dos veces el mismo número de documento
        builder.Entity<Client>()
            .HasIndex(c => c.DocumentNumber)
            .IsUnique();

        // Tampoco puede haber dos clientes con el mismo email
        builder.Entity<Client>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // Si se borra una venta, se borran automáticamente sus detalles (cascade)
        builder.Entity<SaleDetail>()
            .HasOne(sd => sd.Sale)
            .WithMany(s => s.Details)
            .HasForeignKey(sd => sd.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Si se intenta borrar un producto que ya está en una venta, EF lo bloquea (Restrict)
        // Así no perdemos el historial de ventas por accidente
        builder.Entity<SaleDetail>()
            .HasOne(sd => sd.Product)
            .WithMany(p => p.SaleDetails)
            .HasForeignKey(sd => sd.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // PostgreSQL necesita que especifiques el tipo de columna para decimales
        // numeric(18,2) = hasta 18 dígitos en total, con 2 decimales
        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("numeric(18,2)");

        builder.Entity<Sale>()
            .Property(s => s.Total)
            .HasColumnType("numeric(18,2)");

        builder.Entity<SaleDetail>()
            .Property(sd => sd.UnitPrice)
            .HasColumnType("numeric(18,2)");
    }
}
