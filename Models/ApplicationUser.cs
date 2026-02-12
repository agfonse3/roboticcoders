using Microsoft.AspNetCore.Identity;

namespace RoboticCoders.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }

    public ICollection<Course>? TeachingCourses { get; set; }
    public ICollection<CourseTeacherAssignment>? TeacherAssignments { get; set; }
    public ICollection<CourseEnrollment>? Enrollments { get; set; }
}
