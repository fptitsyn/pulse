using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace app.Migrations
{
    /// <inheritdoc />
    public partial class RemovedServiceStatusLookupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "services_service_status_id_fkey",
                table: "services");

            migrationBuilder.DropTable(
                name: "service_statuses");

            migrationBuilder.DropIndex(
                name: "IX_services_service_status_id",
                table: "services");

            migrationBuilder.DropColumn(
                name: "service_status_id",
                table: "services");

            migrationBuilder.AddColumn<int>(
                name: "ServiceStatus",
                table: "services",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceStatus",
                table: "services");

            migrationBuilder.AddColumn<short>(
                name: "service_status_id",
                table: "services",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "service_statuses",
                columns: table => new
                {
                    service_status_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_status = table.Column<string>(type: "character varying", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_statuses_pkey", x => x.service_status_id);
                });

            migrationBuilder.InsertData(
                table: "service_statuses",
                columns: new[] { "service_status_id", "service_status" },
                values: new object[,]
                {
                    { (short)1, "UNKNOWN" },
                    { (short)2, "UP" },
                    { (short)3, "DOWN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_services_service_status_id",
                table: "services",
                column: "service_status_id");

            migrationBuilder.CreateIndex(
                name: "service_statuses_service_status_key",
                table: "service_statuses",
                column: "service_status",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "services_service_status_id_fkey",
                table: "services",
                column: "service_status_id",
                principalTable: "service_statuses",
                principalColumn: "service_status_id");
        }
    }
}
