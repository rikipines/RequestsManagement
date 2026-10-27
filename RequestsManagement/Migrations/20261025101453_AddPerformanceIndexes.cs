using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestsManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestStatusHistories_RequestId",
                table: "RequestStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Status",
                table: "Requests");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedTo",
                table: "Requests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatusHistories_RequestId_ChangedAt",
                table: "RequestStatusHistories",
                columns: new[] { "RequestId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_AssignedTo",
                table: "Requests",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_OrganizationName",
                table: "Requests",
                column: "OrganizationName");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Priority",
                table: "Requests",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Status_CreatedAt",
                table: "Requests",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestStatusHistories_RequestId_ChangedAt",
                table: "RequestStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Requests_AssignedTo",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_OrganizationName",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Priority",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Status_CreatedAt",
                table: "Requests");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedTo",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatusHistories_RequestId",
                table: "RequestStatusHistories",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Status",
                table: "Requests",
                column: "Status");
        }
    }
}
