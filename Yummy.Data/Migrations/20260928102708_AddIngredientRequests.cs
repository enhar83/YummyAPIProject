using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yummy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IngredientRequestId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IngredientRequests",
                columns: table => new
                {
                    IngredientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChefId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChefName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChefNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponseNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HandledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientRequests", x => x.IngredientRequestId);
                    table.ForeignKey(
                        name: "FK_IngredientRequests_Chefs_ChefId",
                        column: x => x.ChefId,
                        principalTable: "Chefs",
                        principalColumn: "ChefId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientRequests_Users_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IngredientRequestItems",
                columns: table => new
                {
                    IngredientRequestItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    SuppliedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientRequestItems", x => x.IngredientRequestItemId);
                    table.ForeignKey(
                        name: "FK_IngredientRequestItems_IngredientRequests_IngredientRequestId",
                        column: x => x.IngredientRequestId,
                        principalTable: "IngredientRequests",
                        principalColumn: "IngredientRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientRequestItems_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "IngredientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IngredientRequestId",
                table: "StockMovements",
                column: "IngredientRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientRequestItems_IngredientId",
                table: "IngredientRequestItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientRequestItems_IngredientRequestId",
                table: "IngredientRequestItems",
                column: "IngredientRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientRequests_ChefId_CreatedDate",
                table: "IngredientRequests",
                columns: new[] { "ChefId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientRequests_HandledByUserId",
                table: "IngredientRequests",
                column: "HandledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientRequests_Status_CreatedDate",
                table: "IngredientRequests",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_IngredientRequests_IngredientRequestId",
                table: "StockMovements",
                column: "IngredientRequestId",
                principalTable: "IngredientRequests",
                principalColumn: "IngredientRequestId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_IngredientRequests_IngredientRequestId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "IngredientRequestItems");

            migrationBuilder.DropTable(
                name: "IngredientRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_IngredientRequestId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "IngredientRequestId",
                table: "StockMovements");
        }
    }
}
