using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class UserSpecifiedNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserSpecifiedNumber",
                table: "Writepads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Writepads_UserSpecifiedNumber_OwnerId",
                table: "Writepads",
                columns: new[] { "UserSpecifiedNumber", "OwnerId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Writepads_UserSpecifiedNumber_OwnerId",
                table: "Writepads");

            migrationBuilder.DropColumn(
                name: "UserSpecifiedNumber",
                table: "Writepads");
        }
    }
}
