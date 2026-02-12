namespace RoboticCoders.ViewModels.Teacher;

public class TeacherStudentProgressViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int CompletedLessons { get; set; }
    public int MissingLessons { get; set; }
    public int ProgressPercent { get; set; }
    public List<string> MissingLessonTitles { get; set; } = new();
}
