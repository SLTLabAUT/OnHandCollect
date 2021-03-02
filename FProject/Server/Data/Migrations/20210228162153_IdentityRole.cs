using Microsoft.EntityFrameworkCore.Migrations;

namespace FProject.Server.Data.Migrations
{
    public partial class IdentityRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "afc9f911-04ae-4bc2-88a6-900ce65eca92", "d3198c4c-4dd9-4d8c-8d35-e76757529aac", "User", "USER" },
                    { "1c6b33d2-a1d8-42fa-924b-43449867f115", "c0a582f7-49de-43f6-9314-d24b0879ce22", "Admin", "ADMIN" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1c6b33d2-a1d8-42fa-924b-43449867f115");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "afc9f911-04ae-4bc2-88a6-900ce65eca92");
        }
    }
}
