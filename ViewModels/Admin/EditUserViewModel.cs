using System.ComponentModel.DataAnnotations;

namespace RoboticCoders.ViewModels.Admin;

public class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }

    [Required]
    public string Role { get; set; } = "Estudiante";
}
