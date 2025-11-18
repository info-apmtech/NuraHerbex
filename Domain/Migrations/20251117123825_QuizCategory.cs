using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class QuizCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orderDetails_OrderId",
                table: "orderDetails",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_orderDetails_Orders_OrderId",
                table: "orderDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orderDetails_Orders_OrderId",
                table: "orderDetails");

            migrationBuilder.DropTable(
                name: "QuizCategories");

            migrationBuilder.DropIndex(
                name: "IX_orderDetails_OrderId",
                table: "orderDetails");
        }
    }
}
