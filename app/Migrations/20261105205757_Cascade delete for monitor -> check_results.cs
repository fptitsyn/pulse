using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app.Migrations
{
    /// <inheritdoc />
    public partial class Cascadedeleteformonitorcheck_results : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "check_results_monitor_id_fkey",
                table: "check_results");

            migrationBuilder.AddForeignKey(
                name: "check_results_monitor_id_fkey",
                table: "check_results",
                column: "monitor_id",
                principalTable: "monitors",
                principalColumn: "monitor_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "check_results_monitor_id_fkey",
                table: "check_results");

            migrationBuilder.AddForeignKey(
                name: "check_results_monitor_id_fkey",
                table: "check_results",
                column: "monitor_id",
                principalTable: "monitors",
                principalColumn: "monitor_id");
        }
    }
}
