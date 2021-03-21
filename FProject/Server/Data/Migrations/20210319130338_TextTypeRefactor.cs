using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class TextTypeRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Text");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Writepads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Rank",
                table: "Text",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Writepads");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Text");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Text",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
