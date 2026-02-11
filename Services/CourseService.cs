
using RoboticCoders.Models;

namespace RoboticCoders.Services
{
    public class CourseService
    {
        public List<CourseSection> GetSections() => new()
        {
            new CourseSection { Name = "Introducción", IsActive = true },
            new CourseSection { Name = "Scratch", IsCompleted = true },
            new CourseSection { Name = "Roblox Studio" }
        };

        public List<Course> GetTools()
        {
            return new()
        {
            new Course { Id = 1, Title = "Scratch", Description = "Programación visual", ImageUrl = "/images/scratch.png" },
            new Course { Id = 2, Title = "Roblox Studio", Description = "Creación de juegos 3D", ImageUrl = "/images/roblox.png" }
        };
        }

    }
}
