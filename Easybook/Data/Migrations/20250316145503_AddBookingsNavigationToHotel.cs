using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Easybook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingsNavigationToHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HotelId1",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HotelId1",
                table: "Bookings",
                column: "HotelId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Hotels_HotelId1",
                table: "Bookings",
                column: "HotelId1",
                principalTable: "Hotels",
                principalColumn: "HotelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Hotels_HotelId1",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_HotelId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "HotelId1",
                table: "Bookings");
        }
    }
}
