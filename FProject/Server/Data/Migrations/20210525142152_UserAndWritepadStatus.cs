using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class UserAndWritepadStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheck",
                table: "Writepads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "BirthYear",
                table: "AspNetUsers",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Education",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheck",
                table: "Writepads");

            migrationBuilder.DropColumn(
                name: "BirthYear",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Education",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "AspNetUsers",
                type: "timestamp without time zone",
                nullable: true);
        }
    }
}
