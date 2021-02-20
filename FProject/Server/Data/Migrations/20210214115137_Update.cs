using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FProject.Server.Migrations
{
    public partial class Update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Text",
                table: "Writepads");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Writepads",
                newName: "TextId");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Writepads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Writepads",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Text",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Text", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Writepads_TextId",
                table: "Writepads",
                column: "TextId");

            migrationBuilder.AddForeignKey(
                name: "FK_Writepads_Text_TextId",
                table: "Writepads",
                column: "TextId",
                principalTable: "Text",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Writepads_Text_TextId",
                table: "Writepads");

            migrationBuilder.DropTable(
                name: "Text");

            migrationBuilder.DropIndex(
                name: "IX_Writepads_TextId",
                table: "Writepads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Writepads");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Writepads");

            migrationBuilder.RenameColumn(
                name: "TextId",
                table: "Writepads",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "Writepads",
                type: "text",
                nullable: true);
        }
    }
}
