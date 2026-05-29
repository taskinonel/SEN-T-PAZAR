using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEN_T_PAZAR.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPopularToListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPopular",
                table: "Listings",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            

            

            migrationBuilder.AddColumn<string>(
                name: "FcmToken",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FcmTokenUpdatedAtUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            // migrationBuilder.AddColumn<bool>(
            //     name: "SmsNotifications",
            //     table: "AspNetUsers",
            //     type: "INTEGER",
            //     nullable: false,
            //     defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "BOOLEAN", nullable: false)
                        ,
                    ActorUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ActorEmail = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListingMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "BOOLEAN", nullable: false)
                        ,
                    ListingId = table.Column<int>(type: "BOOLEAN", nullable: false),
                    SenderUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ReceiverUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingMessages_AspNetUsers_ReceiverUserId",
                        column: x => x.ReceiverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingMessages_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "BOOLEAN", nullable: false)
                        ,
                    ListingId = table.Column<int>(type: "BOOLEAN", nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RecipientUserId = table.Column<string>(type: "TEXT", nullable: true),
                    RecipientPhone = table.Column<string>(type: "TEXT", nullable: true),
                    RecipientEmail = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    SenderUserId = table.Column<string>(type: "TEXT", nullable: true),
                    SenderName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SenderEmail = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    SenderPhone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "BOOLEAN", nullable: false, defaultValue: false),
                    IsRead = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    SenderRole = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingMessages_ListingId",
                table: "ListingMessages",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingMessages_ReceiverUserId",
                table: "ListingMessages",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingMessages_SenderUserId",
                table: "ListingMessages",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "ListingMessages");

            migrationBuilder.DropTable(
                name: "VisitorMessages");

            migrationBuilder.DropColumn(
                name: "IsPopular",
                table: "Listings");

            
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "VisitorMessages");

            
            

            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FcmTokenUpdatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SmsNotifications",
                table: "AspNetUsers");
        }
    }
}


