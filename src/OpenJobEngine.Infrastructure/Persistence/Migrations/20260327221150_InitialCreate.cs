using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJobEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "candidate_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProfessionalSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    YearsOfExperience = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    SeniorityLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    preferred_work_mode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    accept_remote = table.Column<bool>(type: "INTEGER", nullable: false),
                    accept_hybrid = table.Column<bool>(type: "INTEGER", nullable: false),
                    accept_on_site = table.Column<bool>(type: "INTEGER", nullable: false),
                    Preferences_ExcludedWorkModesCsv = table.Column<string>(type: "TEXT", nullable: true),
                    Preferences_IncludedCompanyKeywordsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    Preferences_ExcludedCompanyKeywordsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    salary_expectation_min = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    salary_expectation_max = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    salary_expectation_currency = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    current_city = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    current_region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    current_country_code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    target_cities = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    target_countries = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LocationPreference_TargetTimezonesCsv = table.Column<string>(type: "TEXT", nullable: true),
                    is_willing_to_relocate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    LocationText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EmploymentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SeniorityLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    WorkMode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    TimeZone = table.Column<string>(type: "TEXT", nullable: true),
                    SalaryText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SalaryMin = table.Column<decimal>(type: "TEXT", nullable: true),
                    SalaryMax = table.Column<decimal>(type: "TEXT", nullable: true),
                    SalaryCurrency = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    IsRemote = table.Column<bool>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceJobId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CollectedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualityScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    QualityFlags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_offers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastCollectedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "match_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ResultsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TopScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    AverageScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    HighMatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumMatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LowMatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scrape_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalCollected = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedJobs = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedJobs = table.Column<int>(type: "INTEGER", nullable: false),
                    DeduplicatedJobs = table.Column<int>(type: "INTEGER", nullable: false),
                    DeactivatedJobs = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrape_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candidate_languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LanguageName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Proficiency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_languages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_languages_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SkillSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SkillCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    YearsExperience = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    ProficiencyScore = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_skills_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChannelType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MinimumMatchScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MinimumNewMatchScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    OnlyNewJobs = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastCheckedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profile_alerts_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_searches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RemoteOnly = table.Column<bool>(type: "INTEGER", nullable: true),
                    MinimumSalary = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    MinimumMatchScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MinimumNewMatchScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    OnlyNewJobs = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_searches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_searches_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_offer_history_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobOfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SnapshotHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_offer_history_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_offer_history_entries_job_offers_JobOfferId",
                        column: x => x.JobOfferId,
                        principalTable: "job_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_offer_language_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobOfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LanguageName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MinimumProficiency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_offer_language_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_offer_language_requirements_job_offers_JobOfferId",
                        column: x => x.JobOfferId,
                        principalTable: "job_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_offer_skill_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobOfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SkillSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SkillCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_offer_skill_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_offer_skill_tags_job_offers_JobOfferId",
                        column: x => x.JobOfferId,
                        principalTable: "job_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_offer_source_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobOfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceJobId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SnapshotHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_offer_source_observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_offer_source_observations_job_offers_JobOfferId",
                        column: x => x.JobOfferId,
                        principalTable: "job_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_languages_CandidateProfileId_LanguageCode",
                table: "candidate_languages",
                columns: new[] { "CandidateProfileId", "LanguageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_skills_CandidateProfileId_SkillSlug",
                table: "candidate_skills",
                columns: new[] { "CandidateProfileId", "SkillSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_history_entries_JobOfferId_EventType",
                table: "job_offer_history_entries",
                columns: new[] { "JobOfferId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_history_entries_JobOfferId_OccurredAtUtc",
                table: "job_offer_history_entries",
                columns: new[] { "JobOfferId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_language_requirements_JobOfferId_LanguageCode",
                table: "job_offer_language_requirements",
                columns: new[] { "JobOfferId", "LanguageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_skill_tags_JobOfferId_SkillSlug",
                table: "job_offer_skill_tags",
                columns: new[] { "JobOfferId", "SkillSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_source_observations_JobOfferId_IsActive",
                table: "job_offer_source_observations",
                columns: new[] { "JobOfferId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_source_observations_SourceName_SourceJobId",
                table: "job_offer_source_observations",
                columns: new[] { "SourceName", "SourceJobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_offers_CompanyName",
                table: "job_offers",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_job_offers_DeduplicationKey",
                table: "job_offers",
                column: "DeduplicationKey");

            migrationBuilder.CreateIndex(
                name: "IX_job_offers_LastSeenAtUtc",
                table: "job_offers",
                column: "LastSeenAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_job_offers_SourceName_SourceJobId",
                table: "job_offers",
                columns: new[] { "SourceName", "SourceJobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_offers_Title",
                table: "job_offers",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_job_sources_Name",
                table: "job_sources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_executions_CandidateProfileId",
                table: "match_executions",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_match_executions_CreatedAtUtc",
                table: "match_executions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_profile_alerts_CandidateProfileId",
                table: "profile_alerts",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_searches_CandidateProfileId",
                table: "saved_searches",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_executions_SourceName",
                table: "scrape_executions",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_executions_StartedAtUtc",
                table: "scrape_executions",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_languages");

            migrationBuilder.DropTable(
                name: "candidate_skills");

            migrationBuilder.DropTable(
                name: "job_offer_history_entries");

            migrationBuilder.DropTable(
                name: "job_offer_language_requirements");

            migrationBuilder.DropTable(
                name: "job_offer_skill_tags");

            migrationBuilder.DropTable(
                name: "job_offer_source_observations");

            migrationBuilder.DropTable(
                name: "job_sources");

            migrationBuilder.DropTable(
                name: "match_executions");

            migrationBuilder.DropTable(
                name: "profile_alerts");

            migrationBuilder.DropTable(
                name: "saved_searches");

            migrationBuilder.DropTable(
                name: "scrape_executions");

            migrationBuilder.DropTable(
                name: "job_offers");

            migrationBuilder.DropTable(
                name: "candidate_profiles");
        }
    }
}
