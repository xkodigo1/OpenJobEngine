using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJobEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobOfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MatchScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    MatchBand = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RuleVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Target = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastResponseBody = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DispatchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_deliveries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_deliveries_DispatchedAtUtc",
                table: "alert_deliveries",
                column: "DispatchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_alert_deliveries_ProfileAlertId_JobOfferId",
                table: "alert_deliveries",
                columns: new[] { "ProfileAlertId", "JobOfferId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alert_deliveries_Status",
                table: "alert_deliveries",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_deliveries");
        }
    }
}
