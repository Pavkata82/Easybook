using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Easybook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookedPrideInBookingDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BookedPrice",
                table: "BookingDateRanges",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookedPrice",
                table: "BookingDateRanges");
        }
    }
}
