using RoboticCoders.Models;

namespace RoboticCoders.ViewModels.Student
{
    public class StudentLessonViewModel
    {
        public Lesson Lesson { get; set; } = default!;
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public List<Module> Modules { get; set; } = new();
        public HashSet<int> CompletedLessonIds { get; set; } = new();

        public int ProgressPercent { get; set; }

        public int? NextLessonId { get; set; }

        // (Opcional) si luego quieres navegar lección anterior a nivel de curso
        public int? PrevLessonId { get; set; }

        public bool IsCompleted { get; set; }
    }
}
