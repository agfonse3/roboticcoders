using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboticCoders.Migrations
{
    /// <inheritdoc />
    public partial class AddSlidesEmbedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlidesEmbedUrl",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlidesType",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlidesEmbedUrl",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SlidesType",
                table: "Lessons");
        }
    }
}
