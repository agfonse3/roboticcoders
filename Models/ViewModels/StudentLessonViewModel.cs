namespace RoboticCoders.Models
{
    public class StudentLessonViewModel
    {
        public Lesson Lesson { get; set; } = null!;
        public int? NextLessonId { get; set; }
        public bool IsCompleted { get; set; }
        public int CourseId { get; set; }
    }
}
