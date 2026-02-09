using System;

namespace RoboticCoders.Models
{
    public class LessonProgress
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ApplicationUser User { get; set; }
        public Lesson Lesson { get; set; }
    }
}
