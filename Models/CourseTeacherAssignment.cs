namespace RoboticCoders.Models;

public class CourseTeacherAssignment
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;

    public ICollection<CourseEnrollment> StudentEnrollments { get; set; } = new List<CourseEnrollment>();
}
