namespace RoboticCoders.ViewModels.Admin;

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string Role { get; set; } = "Sin rol";
}
