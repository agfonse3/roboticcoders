using System.ComponentModel.DataAnnotations;

namespace RoboticCoders.Models;

public class LessonHtmlResource
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
