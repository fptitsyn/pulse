using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app.Migrations
{
    /// <inheritdoc />
    public partial class MonitorSchedulingValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "monitors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextCheckedAt",
                table: "monitors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "NextCheckedAt",
                table: "monitors");
        }
    }
}
