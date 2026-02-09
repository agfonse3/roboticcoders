namespace RoboticCoders.Models
{
    public class TeacherCourseDetailsViewModel
    {
        public Course? Course { get; set; }

        public List<TeacherStudentProgressViewModel> Students { get; set; } = new();
    }
}
