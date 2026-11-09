using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app.Migrations
{
    /// <inheritdoc />
    public partial class CheckResultOutcomeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 UPDATE check_results
                                 SET outcome = CASE outcome
                                     WHEN 'SUCCESS' THEN 'Success'
                                     WHEN 'HTTP_ERROR' THEN 'HttpError'
                                     WHEN 'TIMEOUT' THEN 'Timeout'
                                     WHEN 'NETWORK_ERROR' THEN 'NetworkError'
                                     ELSE outcome
                                 END;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
