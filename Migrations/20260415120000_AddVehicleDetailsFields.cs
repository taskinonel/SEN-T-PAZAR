using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEN_T_PAZAR.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDetailsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleCondition",
                table: "Listings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleSteeringType",
                table: "Listings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleCondition",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "VehicleSteeringType",
                table: "Listings");
        }
    }
}
