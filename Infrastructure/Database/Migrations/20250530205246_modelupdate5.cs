using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelupdate5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                schema: "public",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "avatar",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integrations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    configuration = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    credentials = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    synced_record_count = table.Column<int>(type: "integer", nullable: false),
                    last_sync_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_integrations_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_segments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#3b82f6"),
                    customer_count = table.Column<int>(type: "integer", nullable: false),
                    average_churn_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    average_lifetime_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    average_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_segments_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    job_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subscription_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    monthly_recurring_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    lifetime_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    subscription_start_date = table.Column<DateTime>(type: "date", nullable: true),
                    subscription_end_date = table.Column<DateTime>(type: "date", nullable: true),
                    last_login_date = table.Column<DateTime>(type: "timestamp", nullable: true),
                    weekly_login_frequency = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    feature_usage_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    support_ticket_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    churn_risk_score = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    churn_risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Low"),
                    churn_prediction_date = table.Column<DateTime>(type: "timestamp", nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_payment_date = table.Column<DateTime>(type: "date", nullable: true),
                    next_billing_date = table.Column<DateTime>(type: "date", nullable: true),
                    payment_failure_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_payment_failure_date = table.Column<DateTime>(type: "date", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sync_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.ForeignKey(
                        name: "fk_customers_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_metrics",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    metric_date = table.Column<DateTime>(type: "date", nullable: false),
                    total_customers = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    churn_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    revenue_recovered = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    average_lifetime_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    high_risk_customers = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    active_campaigns = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    campaign_success_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboard_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_dashboard_metrics_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    trigger = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    scheduled_date = table.Column<DateTime>(type: "timestamp", nullable: true),
                    sent_date = table.Column<DateTime>(type: "timestamp", nullable: true),
                    sent_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    opened_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    clicked_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    converted_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    revenue_recovered = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaigns_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaigns_customer_segments_segment_id",
                        column: x => x.segment_id,
                        principalSchema: "public",
                        principalTable: "customer_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "segment_criteria",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(20)", maxLength: 20, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_segment_criteria", x => x.id);
                    table.ForeignKey(
                        name: "fk_segment_criteria_customer_segments_segment_id",
                        column: x => x.segment_id,
                        principalSchema: "public",
                        principalTable: "customer_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "churn_predictions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    risk_score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prediction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    risk_factors = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: true),
                    model_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_churn_predictions", x => x.id);
                    table.ForeignKey(
                        name: "fk_churn_predictions_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_activities",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    activity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_activities_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_customers",
                schema: "public",
                columns: table => new
                {
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_customers", x => new { x.campaign_id, x.customer_id });
                    table.ForeignKey(
                        name: "fk_campaign_customers_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalSchema: "public",
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaign_customers_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "public",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_steps",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    delay = table.Column<TimeSpan>(type: "interval", nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sent_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    opened_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    clicked_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    converted_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaign_steps_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalSchema: "public",
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_company_id",
                schema: "public",
                table: "users",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_customers_campaign_id",
                schema: "public",
                table: "campaign_customers",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_customers_customer_id",
                schema: "public",
                table: "campaign_customers",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_steps_campaign_id",
                schema: "public",
                table: "campaign_steps",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_steps_campaign_order",
                schema: "public",
                table: "campaign_steps",
                columns: new[] { "campaign_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_company_id",
                schema: "public",
                table: "campaigns",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_scheduled_date",
                schema: "public",
                table: "campaigns",
                column: "scheduled_date");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_segment_id",
                schema: "public",
                table: "campaigns",
                column: "segment_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_status",
                schema: "public",
                table: "campaigns",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_type",
                schema: "public",
                table: "campaigns",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_churn_predictions_customer_date",
                schema: "public",
                table: "churn_predictions",
                columns: new[] { "customer_id", "prediction_date" });

            migrationBuilder.CreateIndex(
                name: "ix_churn_predictions_customer_id",
                schema: "public",
                table: "churn_predictions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_churn_predictions_prediction_date",
                schema: "public",
                table: "churn_predictions",
                column: "prediction_date");

            migrationBuilder.CreateIndex(
                name: "ix_companies_domain",
                schema: "public",
                table: "companies",
                column: "domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_name",
                schema: "public",
                table: "companies",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_customer_activities_activity_date",
                schema: "public",
                table: "customer_activities",
                column: "activity_date");

            migrationBuilder.CreateIndex(
                name: "ix_customer_activities_customer_date",
                schema: "public",
                table: "customer_activities",
                columns: new[] { "customer_id", "activity_date" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_activities_customer_id",
                schema: "public",
                table: "customer_activities",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_activities_type",
                schema: "public",
                table: "customer_activities",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_customer_segments_company_id",
                schema: "public",
                table: "customer_segments",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_churn_risk_score",
                schema: "public",
                table: "customers",
                column: "churn_risk_score");

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
                name: "ix_customers_email",
                schema: "public",
                table: "customers",
                column: "email");

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

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_metrics_company_date",
                schema: "public",
                table: "dashboard_metrics",
                columns: new[] { "company_id", "metric_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_metrics_company_id",
                schema: "public",
                table: "dashboard_metrics",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_metrics_metric_date",
                schema: "public",
                table: "dashboard_metrics",
                column: "metric_date");

            migrationBuilder.CreateIndex(
                name: "ix_integrations_user_id",
                schema: "public",
                table: "integrations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_segment_criteria_field",
                schema: "public",
                table: "segment_criteria",
                column: "field");

            migrationBuilder.CreateIndex(
                name: "ix_segment_criteria_segment_id",
                schema: "public",
                table: "segment_criteria",
                column: "segment_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_companies_company_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropTable(
                name: "campaign_customers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "campaign_steps",
                schema: "public");

            migrationBuilder.DropTable(
                name: "churn_predictions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customer_activities",
                schema: "public");

            migrationBuilder.DropTable(
                name: "dashboard_metrics",
                schema: "public");

            migrationBuilder.DropTable(
                name: "integrations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "segment_criteria",
                schema: "public");

            migrationBuilder.DropTable(
                name: "campaigns",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "customer_segments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_users_company_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "company_id",
                schema: "public",
                table: "users");

            migrationBuilder.AlterColumn<int>(
                name: "role",
                schema: "public",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "public",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "public",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
