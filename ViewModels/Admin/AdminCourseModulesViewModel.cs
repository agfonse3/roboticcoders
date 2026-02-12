using RoboticCoders.Models;

namespace RoboticCoders.ViewModels.Admin;

public class AdminCourseModulesViewModel
{
    public Course Course { get; set; } = null!;
    public int TotalLessons { get; set; }
    public List<AdminCourseStudentLessonStatusViewModel> Students { get; set; } = new();
}

public class AdminCourseStudentLessonStatusViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int CompletedLessons { get; set; }
    public int MissingLessons { get; set; }
    public int ProgressPercent { get; set; }
    public List<string> MissingLessonTitles { get; set; } = new();
}
