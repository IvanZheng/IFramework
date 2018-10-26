using Microsoft.EntityFrameworkCore.Migrations;

namespace IFramework.Test.Migrations
{
    public partial class AddPersonStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Persons",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Persons");
        }
    }
}
