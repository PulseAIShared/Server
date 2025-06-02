using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelupdate3453 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_integrations_users_user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_users_companies_company_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_integrations_user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.AddColumn<bool>(
                name: "is_company_owner",
                schema: "public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "todo_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "segment_criteria",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                schema: "public",
                table: "integrations",
                type: "uuid",
                maxLength: 36,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "configured_at",
                schema: "public",
                table: "integrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "configured_by_user_id",
                schema: "public",
                table: "integrations",
                type: "uuid",
                maxLength: 36,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "integrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "company_id1",
                schema: "public",
                table: "import_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "import_jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "dashboard_metrics",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "customer_segments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "customer_activities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "public",
                table: "companies",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "companies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "companies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "public",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "max_users",
                schema: "public",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                schema: "public",
                table: "companies",
                type: "uuid",
                maxLength: 36,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "plan",
                schema: "public",
                table: "companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "churn_predictions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "company_id1",
                schema: "public",
                table: "campaigns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "campaigns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                schema: "public",
                table: "campaign_steps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "company_invitations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    invitation_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invited_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_invitations_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_company_invitations_users_accepted_by_user_id",
                        column: x => x.accepted_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_company_invitations_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_integrations_company_id",
                schema: "public",
                table: "integrations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_integrations_company_type",
                schema: "public",
                table: "integrations",
                columns: new[] { "company_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integrations_configured_by_user_id",
                schema: "public",
                table: "integrations",
                column: "configured_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_integrations_status",
                schema: "public",
                table: "integrations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_integrations_type",
                schema: "public",
                table: "integrations",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_company_id1",
                schema: "public",
                table: "import_jobs",
                column: "company_id1");

            migrationBuilder.CreateIndex(
                name: "ix_companies_owner_id",
                schema: "public",
                table: "companies",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_company_id1",
                schema: "public",
                table: "campaigns",
                column: "company_id1");

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_accepted_by_user_id",
                schema: "public",
                table: "company_invitations",
                column: "accepted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_company_id",
                schema: "public",
                table: "company_invitations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_email",
                schema: "public",
                table: "company_invitations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_email_company_accepted",
                schema: "public",
                table: "company_invitations",
                columns: new[] { "email", "company_id", "is_accepted" });

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_expires_at",
                schema: "public",
                table: "company_invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_invited_by_user_id",
                schema: "public",
                table: "company_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_invitations_token",
                schema: "public",
                table: "company_invitations",
                column: "invitation_token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_campaigns_companies_company_id1",
                schema: "public",
                table: "campaigns",
                column: "company_id1",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_companies_users_owner_id",
                schema: "public",
                table: "companies",
                column: "owner_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_import_jobs_companies_company_id1",
                schema: "public",
                table: "import_jobs",
                column: "company_id1",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_integrations_companies_company_id",
                schema: "public",
                table: "integrations",
                column: "company_id",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_integrations_users_configured_by_user_id",
                schema: "public",
                table: "integrations",
                column: "configured_by_user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_users_companies_company_id",
                schema: "public",
                table: "users",
                column: "company_id",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_campaigns_companies_company_id1",
                schema: "public",
                table: "campaigns");

            migrationBuilder.DropForeignKey(
                name: "fk_companies_users_owner_id",
                schema: "public",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "fk_import_jobs_companies_company_id1",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropForeignKey(
                name: "fk_integrations_companies_company_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_integrations_users_configured_by_user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_users_companies_company_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropTable(
                name: "company_invitations",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_integrations_company_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "ix_integrations_company_type",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "ix_integrations_configured_by_user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "ix_integrations_status",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "ix_integrations_type",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "ix_import_jobs_company_id1",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropIndex(
                name: "ix_companies_owner_id",
                schema: "public",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "ix_campaigns_company_id1",
                schema: "public",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "is_company_owner",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "todo_items");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "segment_criteria");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "company_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropColumn(
                name: "configured_at",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropColumn(
                name: "configured_by_user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropColumn(
                name: "company_id1",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "dashboard_metrics");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "customer_segments");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "customer_activities");

            migrationBuilder.DropColumn(
                name: "country",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "max_users",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "owner_id",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "plan",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "churn_predictions");

            migrationBuilder.DropColumn(
                name: "company_id1",
                schema: "public",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "date_created",
                schema: "public",
                table: "campaign_steps");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "public",
                table: "integrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_integrations_user_id",
                schema: "public",
                table: "integrations",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_integrations_users_user_id",
                schema: "public",
                table: "integrations",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_users_companies_company_id",
                schema: "public",
                table: "users",
                column: "company_id",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
