using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEN_T_PAZAR.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedSellerField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerifiedSeller",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByAdminId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerifiedSeller",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerifiedByAdminId",
                table: "AspNetUsers");
        }
    }
}
