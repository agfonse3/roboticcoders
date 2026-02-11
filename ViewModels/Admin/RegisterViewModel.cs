namespace RoboticCoders.ViewModels.Admin
{
    public class RegisterViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Estudiante";
    }
}