using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelupdate563463 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "import_updates",
                schema: "public",
                table: "import_jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "new_records",
                schema: "public",
                table: "import_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "updated_records",
                schema: "public",
                table: "import_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "import_updates",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "new_records",
                schema: "public",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "updated_records",
                schema: "public",
                table: "import_jobs");
        }
    }
}
