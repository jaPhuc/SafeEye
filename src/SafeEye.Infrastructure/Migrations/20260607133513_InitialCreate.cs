using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeEye.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iot_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    firebase_device_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guardian_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guardian_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_guardian_devices_iot_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "iot_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guardian_devices_users_guardian_id",
                        column: x => x.guardian_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "sos_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sos_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_sos_events_iot_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "iot_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sos_events_users_resolved_by_id",
                        column: x => x.resolved_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guardian_devices_device_id",
                table: "guardian_devices",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_guardian_devices_guardian_id_device_id",
                table: "guardian_devices",
                columns: new[] { "guardian_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iot_devices_device_key",
                table: "iot_devices",
                column: "device_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iot_devices_firebase_device_key",
                table: "iot_devices",
                column: "firebase_device_key");

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
                name: "IX_sos_events_device_id",
                table: "sos_events",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_sos_events_resolved_by_id",
                table: "sos_events",
                column: "resolved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_sos_events_status",
                table: "sos_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guardian_devices");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sos_events");

            migrationBuilder.DropTable(
                name: "iot_devices");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
