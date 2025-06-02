using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelupdate345345 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_campaigns_companies_company_id1",
                schema: "public",
                table: "campaigns");

            migrationBuilder.DropForeignKey(
                name: "fk_import_jobs_companies_company_id1",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropIndex(
                name: "ix_import_jobs_company_id1",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropIndex(
                name: "ix_campaigns_company_id1",
                schema: "public",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "company_id1",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "company_id1",
                schema: "public",
                table: "campaigns");

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "refresh_token",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "company_id1",
                schema: "public",
                table: "import_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "company_id1",
                schema: "public",
                table: "campaigns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_company_id1",
                schema: "public",
                table: "import_jobs",
                column: "company_id1");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_company_id1",
                schema: "public",
                table: "campaigns",
                column: "company_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_campaigns_companies_company_id1",
                schema: "public",
                table: "campaigns",
                column: "company_id1",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_import_jobs_companies_company_id1",
                schema: "public",
                table: "import_jobs",
                column: "company_id1",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id");
        }
    }
}
