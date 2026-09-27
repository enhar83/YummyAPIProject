using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yummy.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkChefToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppUserId",
                table: "Chefs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chefs_AppUserId",
                table: "Chefs",
                column: "AppUserId",
                unique: true,
                filter: "[AppUserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Chefs_Users_AppUserId",
                table: "Chefs",
                column: "AppUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chefs_Users_AppUserId",
                table: "Chefs");

            migrationBuilder.DropIndex(
                name: "IX_Chefs_AppUserId",
                table: "Chefs");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Chefs");
        }
    }
}
