using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterEntrySystem.Migrations
{
    /// <inheritdoc />
    public partial class FixTaskSubmissionCascade2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskSubmissions_TaskAssignments_TaskAssignmentId",
                table: "TaskSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSubmissions_Users_WorkerId",
                table: "TaskSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSubmissions_TaskAssignments_TaskAssignmentId",
                table: "TaskSubmissions",
                column: "TaskAssignmentId",
                principalTable: "TaskAssignments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSubmissions_Users_WorkerId",
                table: "TaskSubmissions",
                column: "WorkerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskSubmissions_TaskAssignments_TaskAssignmentId",
                table: "TaskSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSubmissions_Users_WorkerId",
                table: "TaskSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSubmissions_TaskAssignments_TaskAssignmentId",
                table: "TaskSubmissions",
                column: "TaskAssignmentId",
                principalTable: "TaskAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSubmissions_Users_WorkerId",
                table: "TaskSubmissions",
                column: "WorkerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
