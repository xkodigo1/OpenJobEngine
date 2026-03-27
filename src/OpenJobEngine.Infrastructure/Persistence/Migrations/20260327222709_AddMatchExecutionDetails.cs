using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJobEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchExecutionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HardFailureCount",
                table: "match_executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumRequestedScore",
                table: "match_executions",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewHighPriorityCount",
                table: "match_executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PartialMatchCount",
                table: "match_executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StrongMatchCount",
                table: "match_executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalMatchesCount",
                table: "match_executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardFailureCount",
                table: "match_executions");

            migrationBuilder.DropColumn(
                name: "MinimumRequestedScore",
                table: "match_executions");

            migrationBuilder.DropColumn(
                name: "NewHighPriorityCount",
                table: "match_executions");

            migrationBuilder.DropColumn(
                name: "PartialMatchCount",
                table: "match_executions");

            migrationBuilder.DropColumn(
                name: "StrongMatchCount",
                table: "match_executions");

            migrationBuilder.DropColumn(
                name: "TotalMatchesCount",
                table: "match_executions");
        }
    }
}
