using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEN_T_PAZAR.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFullNameToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserFavorites_UserId",
                table: "UserFavorites");

            migrationBuilder.AddColumn<string>(
                name: "UserFullName",
                table: "Payments",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExpiryReminderSent",
                table: "Listings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PopularOrder",
                table: "Listings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishUntil",
                table: "Listings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId_ListingId",
                table: "UserFavorites",
                columns: new[] { "UserId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_UserFavorites_UserId_ListingId",
                table: "UserFavorites");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UserFullName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpiryReminderSent",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PopularOrder",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PublishUntil",
                table: "Listings");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId",
                table: "UserFavorites",
                column: "UserId");
        }
    }
}
