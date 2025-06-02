using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelupdate34534 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_companies_users_owner_id",
                schema: "public",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "max_users",
                schema: "public",
                table: "companies");

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_id",
                schema: "public",
                table: "companies",
                type: "uuid",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 36);

            migrationBuilder.AddForeignKey(
                name: "fk_companies_users_owner_id",
                schema: "public",
                table: "companies",
                column: "owner_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_companies_users_owner_id",
                schema: "public",
                table: "companies");

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_id",
                schema: "public",
                table: "companies",
                type: "uuid",
                maxLength: 36,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_users",
                schema: "public",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "fk_companies_users_owner_id",
                schema: "public",
                table: "companies",
                column: "owner_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
