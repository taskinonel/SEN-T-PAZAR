using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEN_T_PAZAR.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewModerationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsApproved",
                table: "Reviews",
                newName: "ModerationStatus");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                table: "Reviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedByUserId",
                table: "Reviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationNote",
                table: "Reviews",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ModerationNote",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "ModerationStatus",
                table: "Reviews",
                newName: "IsApproved");
        }
    }
}
