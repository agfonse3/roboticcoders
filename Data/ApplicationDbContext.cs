using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Models;

namespace RoboticCoders.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Course> Courses { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<StudentLessonProgress> StudentLessonProgresses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<CourseTeacherAssignment> CourseTeacherAssignments { get; set; }
        public DbSet<LessonHtmlResource> LessonHtmlResources { get; set; }


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<CourseEnrollment>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasIndex(e => new { e.CourseId, e.UserId }).IsUnique();

                b.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

                b.HasOne(e => e.CourseTeacherAssignment)
                    .WithMany(a => a.StudentEnrollments)
                    .HasForeignKey(e => e.CourseTeacherAssignmentId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

                b.HasOne(e => e.User)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
            });

            builder.Entity<CourseTeacherAssignment>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => new { x.CourseId, x.TeacherId }).IsUnique();

                b.HasOne(x => x.Course)
                    .WithMany(c => c.TeacherAssignments)
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

                b.HasOne(x => x.Teacher)
                    .WithMany(u => u.TeacherAssignments)
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
            });
            builder.Entity<LessonHtmlResource>(b =>
{
    b.HasKey(x => x.Id);

    b.HasOne(x => x.Lesson)
     .WithMany(l => l.HtmlResources)
     .HasForeignKey(x => x.LessonId)
     .OnDelete(DeleteBehavior.Cascade);

    b.Property(x => x.Title).HasMaxLength(200);
    b.Property(x => x.OriginalFileName).HasMaxLength(260);
    b.Property(x => x.Url).HasMaxLength(500);
});

        }
    }
}
