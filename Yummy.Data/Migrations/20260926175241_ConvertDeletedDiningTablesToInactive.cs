using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yummy.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDeletedDiningTablesToInactive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // masa silme özelliği kaldırıldı. daha önce soft-delete edilmiş masalar global query filter nedeniyle gizlendiği için
            // bu masalara ait rezervasyonlar da (INNER JOIN) listelerden kayboluyordu. bu masalar silinmemiş fakat pasif hale getirilir.
            migrationBuilder.Sql("UPDATE DiningTables SET IsDeleted = 0, IsActive = 0 WHERE IsDeleted = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // geri alınamaz: hangi pasif masanın daha önce silinmiş olduğu bilgisi tutulmaz.
        }
    }
}
