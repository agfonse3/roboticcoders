using System;

namespace RoboticCoders.Models
{
    public class StudentLessonProgress
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
