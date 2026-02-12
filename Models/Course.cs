using System.Collections.Generic;

namespace RoboticCoders.Models;

public class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    // Legacy field kept for backward compatibility. New flow uses TeacherAssignments.
    public string? TeacherId { get; set; }
    public ApplicationUser? Teacher { get; set; }

    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<CourseTeacherAssignment> TeacherAssignments { get; set; } = new List<CourseTeacherAssignment>();
}
