using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yummy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTableNoToDiningTable : Migration
    {
        // TableNo kolonu nvarchar(max)'tan nvarchar(50)'ye çekilir (index eklenebilmesi için gerekli) ve benzersiz index eklenir.
        // uygulanmadan önce veritabanında tekrar eden veya 50 karakteri aşan masa numarası olmadığı kontrol edilmiştir.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TableNo",
                table: "DiningTables",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_DiningTables_TableNo",
                table: "DiningTables",
                column: "TableNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiningTables_TableNo",
                table: "DiningTables");

            migrationBuilder.AlterColumn<string>(
                name: "TableNo",
                table: "DiningTables",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
