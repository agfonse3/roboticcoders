namespace RoboticCoders.ViewModels.Teacher
{
    public class TeacherCourseViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalLessons { get; set; }
        public int StudentCount { get; set; }
    }
}
