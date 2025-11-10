using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class multiselectForSpecialityid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecalityId",
                table: "DoctorDetails");

            migrationBuilder.AddColumn<string>(
                name: "SpecalityIds",
                table: "DoctorDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecalityIds",
                table: "DoctorDetails");

            migrationBuilder.AddColumn<int>(
                name: "SpecalityId",
                table: "DoctorDetails",
                type: "int",
                nullable: true);
        }
    }
}
