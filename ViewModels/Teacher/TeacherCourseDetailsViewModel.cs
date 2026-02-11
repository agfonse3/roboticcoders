using RoboticCoders.Models;

namespace RoboticCoders.ViewModels.Teacher
{
    public class TeacherCourseDetailsViewModel
    {
        public Course Course { get; set; } = null!;
        public List<TeacherStudentProgressViewModel> Students { get; set; } = new();
    }
}
