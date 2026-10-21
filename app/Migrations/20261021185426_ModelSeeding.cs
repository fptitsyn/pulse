using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace app.Migrations
{
    /// <inheritdoc />
    public partial class ModelSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "service_statuses",
                columns: new[] { "service_status_id", "service_status" },
                values: new object[,]
                {
                    { (short)1, "UNKNOWN" },
                    { (short)2, "UP" },
                    { (short)3, "DOWN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)9);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)10);

            migrationBuilder.DeleteData(
                table: "http_codes",
                keyColumn: "http_code_id",
                keyValue: (short)11);

            migrationBuilder.DeleteData(
                table: "http_methods",
                keyColumn: "http_method_id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "http_methods",
                keyColumn: "http_method_id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "http_methods",
                keyColumn: "http_method_id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "http_methods",
                keyColumn: "http_method_id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "http_methods",
                keyColumn: "http_method_id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "http_methods",
                keyColumn: "http_method_id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "service_statuses",
                keyColumn: "service_status_id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "service_statuses",
                keyColumn: "service_status_id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "service_statuses",
                keyColumn: "service_status_id",
                keyValue: (short)3);
        }
    }
}
