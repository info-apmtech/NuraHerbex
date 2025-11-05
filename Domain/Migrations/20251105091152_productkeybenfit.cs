using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class productkeybenfit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForIndex",
                table: "ProductDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ForThis1",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForThis2",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForThis3",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForThis4",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyBenefits1",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyBenefits2",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyBenefits3",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyBenefits4",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForIndex",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "ForThis1",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "ForThis2",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "ForThis3",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "ForThis4",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "KeyBenefits1",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "KeyBenefits2",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "KeyBenefits3",
                table: "ProductDetails");

            migrationBuilder.DropColumn(
                name: "KeyBenefits4",
                table: "ProductDetails");
        }
    }
}
