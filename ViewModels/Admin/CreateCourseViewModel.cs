using System.ComponentModel.DataAnnotations;

namespace RoboticCoders.ViewModels.Admin;

public class CreateCourseViewModel
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}
