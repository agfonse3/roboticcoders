using System.ComponentModel.DataAnnotations;

namespace RoboticCoders.ViewModels.Admin
{
    public class CreateCourseViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public string? ImageUrl { get; set; }

    }
}
