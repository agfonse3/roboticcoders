using System.Collections.Generic;

namespace RoboticCoders.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ImageUrl { get; set; }

        // 👨‍🏫 Docente asignado
        public string? TeacherId { get; set; }
        public ApplicationUser? Teacher { get; set; }

        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<CourseEnrollment>? Enrollments { get; set; }
    }
}
