using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Yummy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToDiningTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "DiningTableId",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "DiningTables",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "DiningTables");

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
        }
    }
}
