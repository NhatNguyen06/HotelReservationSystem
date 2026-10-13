using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingCode = table.Column<string>(type: "TEXT", nullable: false),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Guests = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    HoldExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Reservations",
                columns: new[] { "Id", "BookingCode", "CheckIn", "CheckOut", "Guests", "HoldExpiresAt", "RoomId", "Status", "TotalCost" },
                values: new object[,]
                {
                    { 1, "BK-SEED01", new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 23), 2, null, 1, 1, 1500000m },
                    { 2, "BK-SEED02", new DateOnly(2026, 10, 21), new DateOnly(2026, 10, 24), 3, null, 4, 1, 2700000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookingCode",
                table: "Reservations",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId",
                table: "Reservations",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");
        }
    }
}
