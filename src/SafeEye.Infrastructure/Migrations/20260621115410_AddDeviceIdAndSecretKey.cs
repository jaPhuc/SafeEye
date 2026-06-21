using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeEye.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceIdAndSecretKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "device_id",
                table: "iot_devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "secret_key_hash",
                table: "iot_devices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_iot_devices_device_id",
                table: "iot_devices",
                column: "device_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_iot_devices_device_id",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "device_id",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "secret_key_hash",
                table: "iot_devices");
        }
    }
}
