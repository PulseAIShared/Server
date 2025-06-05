using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MultiSourceCustomerData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_company_external_source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_company_id",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_last_login",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_payment_status",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_subscription_status",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "external_id",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "feature_usage_percentage",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "last_login_date",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "last_payment_date",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "last_payment_failure_date",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "lifetime_value",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "monthly_recurring_revenue",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "next_billing_date",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "payment_failure_count",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "payment_status",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "plan",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "subscription_end_date",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "subscription_start_date",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "subscription_status",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "support_ticket_count",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "weekly_login_frequency",
                schema: "public",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "sync_version",
                schema: "public",
                table: "customers",
                newName: "primary_support_source");

            migrationBuilder.AlterColumn<string>(
                name: "time_zone",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_synced_at",
                schema: "public",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "job_title",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "company_name",
                schema: "public",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "churn_risk_level",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Low");

            migrationBuilder.AlterColumn<DateTime>(
                name: "churn_prediction_date",
                schema: "public",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_crm_source",
                schema: "public",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_engagement_source",
                schema: "public",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_marketing_source",
                schema: "public",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_payment_source",
                schema: "public",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_crm_data",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary_source = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    source_priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lead_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lifecycle_stage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lead_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sales_owner_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sales_owner_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_contact_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_contact_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deal_count = table.Column<int>(type: "integer", nullable: false),
                    total_deal_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    won_deal_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    import_batch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    sync_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    custom_fields = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_crm_data", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_crm_data_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_crm_data_users_imported_by_user_id",
                        column: x => x.imported_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_engagement_data",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary_source = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    source_priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    first_login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    weekly_login_frequency = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    monthly_login_frequency = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    average_session_duration = table.Column<double>(type: "numeric(8,2)", nullable: false, defaultValue: 0.0),
                    feature_usage_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    feature_usage_counts = table.Column<string>(type: "jsonb", nullable: true),
                    last_feature_usage = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    page_views = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bounce_rate = table.Column<double>(type: "numeric(5,4)", nullable: false, defaultValue: 0.0),
                    most_visited_pages = table.Column<string>(type: "jsonb", nullable: false),
                    custom_events = table.Column<string>(type: "jsonb", nullable: true),
                    import_batch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sync_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_engagement_data", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_engagement_data_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_engagement_data_users_imported_by_user_id",
                        column: x => x.imported_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_marketing_data",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary_source = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    source_priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_subscribed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    subscription_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unsubscription_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    average_open_rate = table.Column<double>(type: "numeric(5,4)", nullable: false, defaultValue: 0.0),
                    average_click_rate = table.Column<double>(type: "numeric(5,4)", nullable: false, defaultValue: 0.0),
                    total_emails_sent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_emails_opened = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_emails_clicked = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_email_open_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_email_click_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    campaign_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_campaign_engagement = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
                    lists = table.Column<string>(type: "jsonb", nullable: false),
                    import_batch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sync_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_marketing_data", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_marketing_data_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_marketing_data_users_imported_by_user_id",
                        column: x => x.imported_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_payment_data",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary_source = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    source_priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    subscription_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    monthly_recurring_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    lifetime_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    subscription_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subscription_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_billing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_failure_count = table.Column<int>(type: "integer", nullable: false),
                    last_payment_failure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_method_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    current_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    billing_interval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    import_batch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_payment_data", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_payment_data_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_payment_data_users_imported_by_user_id",
                        column: x => x.imported_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_support_data",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary_source = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    source_priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    open_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    closed_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    first_ticket_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_ticket_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    average_resolution_time = table.Column<double>(type: "numeric(8,2)", nullable: false, defaultValue: 0.0),
                    customer_satisfaction_score = table.Column<double>(type: "numeric(3,2)", nullable: false, defaultValue: 0.0),
                    low_priority_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    medium_priority_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    high_priority_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    urgent_tickets = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tickets_by_category = table.Column<string>(type: "jsonb", nullable: true),
                    import_batch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sync_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_support_data", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_support_data_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_support_data_users_imported_by_user_id",
                        column: x => x.imported_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_email",
                schema: "public",
                table: "customers",
                columns: new[] { "company_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_primary_crm_source",
                schema: "public",
                table: "customers",
                column: "primary_crm_source");

            migrationBuilder.CreateIndex(
                name: "ix_customers_primary_payment_source",
                schema: "public",
                table: "customers",
                column: "primary_payment_source");

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_customer_id",
                schema: "public",
                table: "customer_crm_data",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_customer_primary",
                schema: "public",
                table: "customer_crm_data",
                columns: new[] { "customer_id", "is_primary_source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_customer_source",
                schema: "public",
                table: "customer_crm_data",
                columns: new[] { "customer_id", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_import_batch",
                schema: "public",
                table: "customer_crm_data",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_imported_by_user_id",
                schema: "public",
                table: "customer_crm_data",
                column: "imported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_is_active",
                schema: "public",
                table: "customer_crm_data",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_customer_crm_data_source_external_id",
                schema: "public",
                table: "customer_crm_data",
                columns: new[] { "source", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_activity_usage",
                schema: "public",
                table: "customer_engagement_data",
                columns: new[] { "weekly_login_frequency", "feature_usage_percentage" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_avg_session_duration",
                schema: "public",
                table: "customer_engagement_data",
                column: "average_session_duration");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_customer_id",
                schema: "public",
                table: "customer_engagement_data",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_customer_last_login",
                schema: "public",
                table: "customer_engagement_data",
                columns: new[] { "customer_id", "last_login_date" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_customer_primary",
                schema: "public",
                table: "customer_engagement_data",
                columns: new[] { "customer_id", "is_primary_source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_customer_source",
                schema: "public",
                table: "customer_engagement_data",
                columns: new[] { "customer_id", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_feature_usage",
                schema: "public",
                table: "customer_engagement_data",
                column: "feature_usage_percentage");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_import_batch",
                schema: "public",
                table: "customer_engagement_data",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_imported_by_user_id",
                schema: "public",
                table: "customer_engagement_data",
                column: "imported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_is_active",
                schema: "public",
                table: "customer_engagement_data",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_last_login",
                schema: "public",
                table: "customer_engagement_data",
                column: "last_login_date");

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_source_external_id",
                schema: "public",
                table: "customer_engagement_data",
                columns: new[] { "source", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_engagement_data_weekly_login_freq",
                schema: "public",
                table: "customer_engagement_data",
                column: "weekly_login_frequency");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_customer_id",
                schema: "public",
                table: "customer_marketing_data",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_customer_primary",
                schema: "public",
                table: "customer_marketing_data",
                columns: new[] { "customer_id", "is_primary_source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_customer_source",
                schema: "public",
                table: "customer_marketing_data",
                columns: new[] { "customer_id", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_import_batch",
                schema: "public",
                table: "customer_marketing_data",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_imported_by_user_id",
                schema: "public",
                table: "customer_marketing_data",
                column: "imported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_is_active",
                schema: "public",
                table: "customer_marketing_data",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_is_subscribed",
                schema: "public",
                table: "customer_marketing_data",
                column: "is_subscribed");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_last_email_open",
                schema: "public",
                table: "customer_marketing_data",
                column: "last_email_open_date");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_open_rate",
                schema: "public",
                table: "customer_marketing_data",
                column: "average_open_rate");

            migrationBuilder.CreateIndex(
                name: "ix_customer_marketing_data_source_external_id",
                schema: "public",
                table: "customer_marketing_data",
                columns: new[] { "source", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_customer_id",
                schema: "public",
                table: "customer_payment_data",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_customer_primary",
                schema: "public",
                table: "customer_payment_data",
                columns: new[] { "customer_id", "is_primary_source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_customer_source",
                schema: "public",
                table: "customer_payment_data",
                columns: new[] { "customer_id", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_import_batch",
                schema: "public",
                table: "customer_payment_data",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_imported_by_user_id",
                schema: "public",
                table: "customer_payment_data",
                column: "imported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_payment_status",
                schema: "public",
                table: "customer_payment_data",
                column: "payment_status");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_source_external_id",
                schema: "public",
                table: "customer_payment_data",
                columns: new[] { "source", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_data_subscription_status",
                schema: "public",
                table: "customer_payment_data",
                column: "subscription_status");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_csat_score",
                schema: "public",
                table: "customer_support_data",
                column: "customer_satisfaction_score");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_customer_id",
                schema: "public",
                table: "customer_support_data",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_customer_primary",
                schema: "public",
                table: "customer_support_data",
                columns: new[] { "customer_id", "is_primary_source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_customer_source",
                schema: "public",
                table: "customer_support_data",
                columns: new[] { "customer_id", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_import_batch",
                schema: "public",
                table: "customer_support_data",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_imported_by_user_id",
                schema: "public",
                table: "customer_support_data",
                column: "imported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_is_active",
                schema: "public",
                table: "customer_support_data",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_last_ticket",
                schema: "public",
                table: "customer_support_data",
                column: "last_ticket_date");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_open_tickets",
                schema: "public",
                table: "customer_support_data",
                column: "open_tickets");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_data_source_external_id",
                schema: "public",
                table: "customer_support_data",
                columns: new[] { "source", "external_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_crm_data",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customer_engagement_data",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customer_marketing_data",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customer_payment_data",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customer_support_data",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_customers_company_email",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_primary_crm_source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_primary_payment_source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "primary_crm_source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "primary_engagement_source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "primary_marketing_source",
                schema: "public",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "primary_payment_source",
                schema: "public",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "primary_support_source",
                schema: "public",
                table: "customers",
                newName: "sync_version");

            migrationBuilder.AlterColumn<string>(
                name: "time_zone",
                schema: "public",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                schema: "public",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_synced_at",
                schema: "public",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "job_title",
                schema: "public",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                schema: "public",
                table: "customers",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "company_name",
                schema: "public",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "churn_risk_level",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Low",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "churn_prediction_date",
                schema: "public",
                table: "customers",
                type: "timestamp",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                schema: "public",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "feature_usage_percentage",
                schema: "public",
                table: "customers",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login_date",
                schema: "public",
                table: "customers",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_payment_date",
                schema: "public",
                table: "customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_payment_failure_date",
                schema: "public",
                table: "customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lifetime_value",
                schema: "public",
                table: "customers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "monthly_recurring_revenue",
                schema: "public",
                table: "customers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_billing_date",
                schema: "public",
                table: "customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_failure_count",
                schema: "public",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "plan",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "public",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_end_date",
                schema: "public",
                table: "customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_start_date",
                schema: "public",
                table: "customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_status",
                schema: "public",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "support_ticket_count",
                schema: "public",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "weekly_login_frequency",
                schema: "public",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_external_source",
                schema: "public",
                table: "customers",
                columns: new[] { "company_id", "external_id", "source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_id",
                schema: "public",
                table: "customers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_last_login",
                schema: "public",
                table: "customers",
                column: "last_login_date");

            migrationBuilder.CreateIndex(
                name: "ix_customers_payment_status",
                schema: "public",
                table: "customers",
                column: "payment_status");

            migrationBuilder.CreateIndex(
                name: "ix_customers_subscription_status",
                schema: "public",
                table: "customers",
                column: "subscription_status");
        }
    }
}
