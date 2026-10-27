using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestsManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationPriorityIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Requests_OrganizationName_Priority_CreatedAt_Id",
                table: "Requests",
                columns: new[] { "OrganizationName", "Priority", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_OrganizationName_Priority_CreatedAt_Id",
                table: "Requests");
        }
    }
}
