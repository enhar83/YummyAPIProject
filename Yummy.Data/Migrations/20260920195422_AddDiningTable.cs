using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Yummy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiningTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Reservations;");
            
            migrationBuilder.AddColumn<Guid>(
                name: "DiningTableId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ReservationEndTime",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DiningTables",
                columns: table => new
                {
                    DiningTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningTables", x => x.DiningTableId);
                });

            migrationBuilder.InsertData(
                table: "DiningTables",
                columns: new[] { "DiningTableId", "Capacity", "IsActive", "TableNo" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 2, true, "Masa 1" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 2, true, "Masa 2" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 4, true, "Masa 3" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 4, true, "Masa 4" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 4, true, "Masa 5" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 6, true, "Masa 6" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), 6, true, "Masa 7" },
                    { new Guid("88888888-8888-8888-8888-888888888888"), 8, true, "Masa 8" },
                    { new Guid("99999999-9999-9999-9999-999999999999"), 10, true, "VIP 1" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 12, true, "VIP 2" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DiningTableId",
                table: "Reservations",
                column: "DiningTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_DiningTables_DiningTableId",
                table: "Reservations",
                column: "DiningTableId",
                principalTable: "DiningTables",
                principalColumn: "DiningTableId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_DiningTables_DiningTableId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "DiningTables");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_DiningTableId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DiningTableId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationEndTime",
                table: "Reservations");
        }
    }
}
