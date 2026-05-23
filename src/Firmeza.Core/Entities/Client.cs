namespace Firmeza.Core.Entities;

public class Client
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    // Tipo de documento (CC, NIT, CE, etc.)
    public string DocumentType { get; set; } = string.Empty;

    // Número de documento (único)
    public string DocumentNumber { get; set; } = string.Empty;

    // Correo electrónico (único)
    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    // Edad en años
    public int Age { get; set; }

    // Fecha de registro
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Historial de compras
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();

    // Nombre completo concatenado
    public string FullName => $"{FirstName} {LastName}";
}
