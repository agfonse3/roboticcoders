namespace RoboticCoders.ViewModels.Admin;

public class AdminTeacherCourseProgressViewModel
{
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public int ProgressPercent { get; set; }
}
