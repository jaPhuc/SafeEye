using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeEye.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_guardian_devices_users_guardian_id",
                table: "guardian_devices");

            migrationBuilder.DropForeignKey(
                name: "FK_sos_events_users_resolved_by_id",
                table: "sos_events");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_sos_events_resolved_by_id",
                table: "sos_events");

            migrationBuilder.DropIndex(
                name: "IX_iot_devices_firebase_user_id",
                table: "iot_devices");

            migrationBuilder.DropIndex(
                name: "IX_guardian_devices_guardian_id_device_id",
                table: "guardian_devices");

            migrationBuilder.DropColumn(
                name: "resolved_by_id",
                table: "sos_events");

            migrationBuilder.DropColumn(
                name: "firebase_user_id",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "guardian_id",
                table: "guardian_devices");

            migrationBuilder.AddColumn<string>(
                name: "resolved_by_guardian_uuid",
                table: "sos_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fcm_token",
                table: "guardian_devices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "guardian_uuid",
                table: "guardian_devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_guardian_devices_guardian_uuid",
                table: "guardian_devices",
                column: "guardian_uuid");

            migrationBuilder.CreateIndex(
                name: "IX_guardian_devices_guardian_uuid_device_id",
                table: "guardian_devices",
                columns: new[] { "guardian_uuid", "device_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_guardian_devices_guardian_uuid",
                table: "guardian_devices");

            migrationBuilder.DropIndex(
                name: "IX_guardian_devices_guardian_uuid_device_id",
                table: "guardian_devices");

            migrationBuilder.DropColumn(
                name: "resolved_by_guardian_uuid",
                table: "sos_events");

            migrationBuilder.DropColumn(
                name: "fcm_token",
                table: "guardian_devices");

            migrationBuilder.DropColumn(
                name: "guardian_uuid",
                table: "guardian_devices");

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_id",
                table: "sos_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "firebase_user_id",
                table: "iot_devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "guardian_id",
                table: "guardian_devices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    google_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sos_events_resolved_by_id",
                table: "sos_events",
                column: "resolved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_devices_firebase_user_id",
                table: "iot_devices",
                column: "firebase_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_guardian_devices_guardian_id_device_id",
                table: "guardian_devices",
                columns: new[] { "guardian_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_google_id",
                table: "users",
                column: "google_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_guardian_devices_users_guardian_id",
                table: "guardian_devices",
                column: "guardian_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sos_events_users_resolved_by_id",
                table: "sos_events",
                column: "resolved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
