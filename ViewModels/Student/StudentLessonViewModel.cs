using RoboticCoders.Models;

namespace RoboticCoders.ViewModels.Student;

public class StudentLessonViewModel
{
    public Lesson Lesson { get; set; } = null!;
    public int? NextLessonId { get; set; }
    public bool IsCompleted { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public List<Module> Modules { get; set; } = new();
    public HashSet<int> CompletedLessonIds { get; set; } = new();
    public int ProgressPercent { get; set; }
}
