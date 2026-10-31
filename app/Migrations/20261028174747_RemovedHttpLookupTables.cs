using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace app.Migrations
{
    /// <inheritdoc />
    public partial class RemovedHttpLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "check_results_http_code_id_fkey",
                table: "check_results");

            migrationBuilder.DropForeignKey(
                name: "monitors_http_method_id_fkey",
                table: "monitors");

            migrationBuilder.DropTable(
                name: "http_codes");

            migrationBuilder.DropTable(
                name: "http_methods");

            migrationBuilder.DropIndex(
                name: "IX_monitors_http_method_id",
                table: "monitors");

            migrationBuilder.DropIndex(
                name: "IX_check_results_http_code_id",
                table: "check_results");

            migrationBuilder.DropColumn(
                name: "http_method_id",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "http_code_id",
                table: "check_results");

            migrationBuilder.RenameColumn(
                name: "ServiceStatus",
                table: "services",
                newName: "service_status");

            migrationBuilder.AlterColumn<string>(
                name: "service_status",
                table: "services",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "http_method",
                table: "monitors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "http_status_code",
                table: "check_results",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "http_method",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "http_status_code",
                table: "check_results");

            migrationBuilder.RenameColumn(
                name: "service_status",
                table: "services",
                newName: "ServiceStatus");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceStatus",
                table: "services",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<short>(
                name: "http_method_id",
                table: "monitors",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "http_code_id",
                table: "check_results",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "http_codes",
                columns: table => new
                {
                    http_code_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    http_code = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("http_codes_pkey", x => x.http_code_id);
                });

            migrationBuilder.CreateTable(
                name: "http_methods",
                columns: table => new
                {
                    http_method_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    http_method = table.Column<string>(type: "character varying", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("http_methods_pkey", x => x.http_method_id);
                });

            migrationBuilder.InsertData(
                table: "http_codes",
                columns: new[] { "http_code_id", "http_code" },
                values: new object[,]
                {
                    { (short)1, (short)200 },
                    { (short)2, (short)201 },
                    { (short)3, (short)204 },
                    { (short)4, (short)301 },
                    { (short)5, (short)302 },
                    { (short)6, (short)304 },
                    { (short)7, (short)400 },
                    { (short)8, (short)401 },
                    { (short)9, (short)403 },
                    { (short)10, (short)404 },
                    { (short)11, (short)500 }
                });

            migrationBuilder.InsertData(
                table: "http_methods",
                columns: new[] { "http_method_id", "http_method" },
                values: new object[,]
                {
                    { (short)1, "HEAD" },
                    { (short)2, "GET" },
                    { (short)3, "POST" },
                    { (short)4, "PUT" },
                    { (short)5, "PATCH" },
                    { (short)6, "DELETE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_monitors_http_method_id",
                table: "monitors",
                column: "http_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_check_results_http_code_id",
                table: "check_results",
                column: "http_code_id");

            migrationBuilder.CreateIndex(
                name: "http_codes_http_code_key",
                table: "http_codes",
                column: "http_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "http_methods_http_method_key",
                table: "http_methods",
                column: "http_method",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "check_results_http_code_id_fkey",
                table: "check_results",
                column: "http_code_id",
                principalTable: "http_codes",
                principalColumn: "http_code_id");

            migrationBuilder.AddForeignKey(
                name: "monitors_http_method_id_fkey",
                table: "monitors",
                column: "http_method_id",
                principalTable: "http_methods",
                principalColumn: "http_method_id");
        }
    }
}
