using System.ComponentModel.DataAnnotations;

namespace RoboticCoders.Models
{
    public class CreateCourseViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public string? ImageUrl { get; set; }

        [Required]
        public string TeacherId { get; set; } = "";
    }
}
