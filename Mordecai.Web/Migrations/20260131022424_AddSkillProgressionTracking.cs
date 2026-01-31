using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillProgressionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "SkillUsageDailyTracking",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    TrackingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillUsageDailyTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillUsageDailyTracking_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillUsageHourlyTracking",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    WindowStartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillUsageHourlyTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillUsageHourlyTracking_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillUsageTargetCooldowns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LastCountedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillUsageTargetCooldowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillUsageTargetCooldowns_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageDailyTracking_CharacterId_SkillDefinitionId_Track~",
                table: "SkillUsageDailyTracking",
                columns: new[] { "CharacterId", "SkillDefinitionId", "TrackingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageDailyTracking_SkillDefinitionId",
                table: "SkillUsageDailyTracking",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageDailyTracking_TrackingDate",
                table: "SkillUsageDailyTracking",
                column: "TrackingDate");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageHourlyTracking_CharacterId_SkillDefinitionId_Wind~",
                table: "SkillUsageHourlyTracking",
                columns: new[] { "CharacterId", "SkillDefinitionId", "WindowStartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageHourlyTracking_SkillDefinitionId",
                table: "SkillUsageHourlyTracking",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageHourlyTracking_WindowStartTime",
                table: "SkillUsageHourlyTracking",
                column: "WindowStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageTargetCooldowns_CharacterId_SkillDefinitionId_Tar~",
                table: "SkillUsageTargetCooldowns",
                columns: new[] { "CharacterId", "SkillDefinitionId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageTargetCooldowns_LastCountedAt",
                table: "SkillUsageTargetCooldowns",
                column: "LastCountedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageTargetCooldowns_SkillDefinitionId",
                table: "SkillUsageTargetCooldowns",
                column: "SkillDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillUsageDailyTracking");

            migrationBuilder.DropTable(
                name: "SkillUsageHourlyTracking");

            migrationBuilder.DropTable(
                name: "SkillUsageTargetCooldowns");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
