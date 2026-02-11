using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboticCoders.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedAtToStudentLessonProgresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "StudentLessonProgresses",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "StudentLessonProgresses");
        }
    }
}
