using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeEye.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFirebaseUserIdAndBattery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "battery_percent",
                table: "iot_devices",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "firebase_user_id",
                table: "iot_devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "uptime_seconds",
                table: "iot_devices",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_iot_devices_firebase_user_id",
                table: "iot_devices",
                column: "firebase_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_iot_devices_firebase_user_id",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "battery_percent",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "firebase_user_id",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "uptime_seconds",
                table: "iot_devices");
        }
    }
}
