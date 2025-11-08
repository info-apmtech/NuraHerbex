using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class consultationchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookingDetails_AspNetUsers_PreferredDoctorId",
                table: "ConsultationBookingDetails");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationBookingDetails_PreferredDoctorId",
                table: "ConsultationBookingDetails");

            migrationBuilder.AlterColumn<string>(
                name: "PreferredDoctorId",
                table: "ConsultationBookingDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredDoctorId",
                table: "ConsultationBookingDetails",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookingDetails_PreferredDoctorId",
                table: "ConsultationBookingDetails",
                column: "PreferredDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookingDetails_AspNetUsers_PreferredDoctorId",
                table: "ConsultationBookingDetails",
                column: "PreferredDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
