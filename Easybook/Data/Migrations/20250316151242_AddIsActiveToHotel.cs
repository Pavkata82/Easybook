using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Easybook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Hotels",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Hotels");
        }
    }
}
