using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class OptionalText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Writepads_Text_TextId",
                table: "Writepads");

            migrationBuilder.AlterColumn<int>(
                name: "TextId",
                table: "Writepads",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Writepads_Text_TextId",
                table: "Writepads",
                column: "TextId",
                principalTable: "Text",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Writepads_Text_TextId",
                table: "Writepads");

            migrationBuilder.AlterColumn<int>(
                name: "TextId",
                table: "Writepads",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Writepads_Text_TextId",
                table: "Writepads",
                column: "TextId",
                principalTable: "Text",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
