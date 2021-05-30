using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class HandRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed null values with 0 to prevent null error on applying migration
            migrationBuilder.UpdateData(
                table: "Writepads",
                keyColumn: "Hand",
                keyValue: null,
                column: "Hand",
                value: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Hand",
                table: "Writepads",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Hand",
                table: "Writepads",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
