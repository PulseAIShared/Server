using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelupdate44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "import_jobs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    import_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    total_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    successful_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    skipped_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    validation_errors = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    import_summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_jobs_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_import_jobs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    action_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_company_id",
                schema: "public",
                table: "import_jobs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_created_at",
                schema: "public",
                table: "import_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_status",
                schema: "public",
                table: "import_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_status_created",
                schema: "public",
                table: "import_jobs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_type",
                schema: "public",
                table: "import_jobs",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_user_id",
                schema: "public",
                table: "import_jobs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_user_status",
                schema: "public",
                table: "import_jobs",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_category",
                schema: "public",
                table: "notifications",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_created_at",
                schema: "public",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_expires_at",
                schema: "public",
                table: "notifications",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_is_read",
                schema: "public",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_created",
                schema: "public",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                schema: "public",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_read",
                schema: "public",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "public");

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
