using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class NewTextTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Priority",
                table: "Text",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Text");
        }
    }
}
