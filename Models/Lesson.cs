namespace RoboticCoders.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? SlideUrl { get; set; }
        public string? VideoUrl { get; set; }

        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        public List<LessonHtmlResource> HtmlResources { get; set; } = new();

    }
}
