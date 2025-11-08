using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class DoctorDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialties",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreferredDate",
                table: "ConsultationBookingDetails",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "DoctorDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimarySpecality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecalityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsWorking = table.Column<bool>(type: "bit", nullable: false),
                    MondayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    MondayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    TuesdayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    TuesdayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    WednesdayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    WednesdayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ThursdayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ThursdayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    FridayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    FridayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    SaturdayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    SaturdayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    SundayStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    SundayEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorDetails", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorDetails");

            migrationBuilder.DropColumn(
                name: "PreferredDate",
                table: "ConsultationBookingDetails");

            migrationBuilder.AddColumn<int>(
                name: "Specialties",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }
    }
}
