using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboticCoders.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTeacherAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments");

            migrationBuilder.AddColumn<int>(
                name: "CourseTeacherAssignmentId",
                table: "CourseEnrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseTeacherAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTeacherAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseTeacherAssignments_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseTeacherAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId_UserId",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseTeacherAssignmentId",
                table: "CourseEnrollments",
                column: "CourseTeacherAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTeacherAssignments_CourseId_TeacherId",
                table: "CourseTeacherAssignments",
                columns: new[] { "CourseId", "TeacherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseTeacherAssignments_TeacherId",
                table: "CourseTeacherAssignments",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CourseTeacherAssignments_CourseTeacherAssignmentId",
                table: "CourseEnrollments",
                column: "CourseTeacherAssignmentId",
                principalTable: "CourseTeacherAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CourseTeacherAssignments_CourseTeacherAssignmentId",
                table: "CourseEnrollments");

            migrationBuilder.DropTable(
                name: "CourseTeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseId_UserId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseTeacherAssignmentId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "CourseTeacherAssignmentId",
                table: "CourseEnrollments");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments",
                column: "CourseId");
        }
    }
}
