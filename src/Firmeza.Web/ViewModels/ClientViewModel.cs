using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.ViewModels;

public class ClientViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document type is required")]
    [Display(Name = "Document Type")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document number is required")]
    [StringLength(20)]
    [Display(Name = "Document Number")]
    public string DocumentNumber { get; set; } = string.Empty;

    // Valida formato de correo electrónico
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    // Valida formato telefónico
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    // Entrada de edad como texto para validación manual
    [Display(Name = "Age")]
    public string AgeInput { get; set; } = string.Empty;

    // Edad numérica convertida
    public int Age { get; set; }

    public static List<string> DocumentTypes => ["CC", "NIT", "CE", "Passport"];
}
