using RoboticCoders.Models;

namespace RoboticCoders.ViewModels.Admin;

public class ManageCourseViewModel
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public List<string> SelectedTeacherIds { get; set; } = new();
    public List<ApplicationUser> Teachers { get; set; } = new();
    public List<StudentTeacherAssignmentOptionViewModel> Students { get; set; } = new();
}

public class StudentTeacherAssignmentOptionViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? AssignedTeacherId { get; set; }
    public int ProgressPercent { get; set; }
}
