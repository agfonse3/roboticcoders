using Microsoft.AspNetCore.Identity;

namespace RoboticCoders.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Aquí luego puedes agregar:
        // Edad
        // Tipo de usuario (Estudiante / Padre)
        public ICollection<Course>? TeachingCourses { get; set; }
        public ICollection<CourseEnrollment>? Enrollments { get; set; }
    }
}
