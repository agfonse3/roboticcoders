using RoboticCoders.Models;

namespace RoboticCoders.ViewModels.Teacher;

public class TeacherCourseDetailsViewModel
{
    public Course? Course { get; set; }
    public List<TeacherStudentProgressViewModel> Students { get; set; } = new();
    public HashSet<int> ReviewedLessonIds { get; set; } = new();
}
