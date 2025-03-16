using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Easybook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeBehaviourOnHotelToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, drop the old foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Hotels_HotelId", // The name of the existing foreign key
                table: "Bookings");

            // Then, add the new foreign key constraint with cascading delete
            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Hotels_HotelId", // The same name for the foreign key
                table: "Bookings",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "HotelId",
                onDelete: ReferentialAction.Cascade); // Change to Cascade for cascading delete
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // If rolling back, we reverse the above operations
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Hotels_HotelId",
                table: "Bookings");

            // Revert to Restrict delete behavior
            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Hotels_HotelId",
                table: "Bookings",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "HotelId",
                onDelete: ReferentialAction.Restrict); // Revert to Restrict
        }
    }
}
