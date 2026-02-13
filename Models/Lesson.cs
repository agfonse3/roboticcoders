namespace RoboticCoders.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? SlideUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? SlidesEmbedUrl { get; set; }   // Canva u otro embed
        public string? SlidesType { get; set; }       // "File" o "Embed"


        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        public List<LessonHtmlResource> HtmlResources { get; set; } = new();

    }
}
