using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentFatigue",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoomId",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVitality",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingFatigueDamage",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingVitalityDamage",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DefaultBaseCost = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultMultiplier = table.Column<decimal>(type: "TEXT", nullable: false),
                    AllowsPassiveAdvancement = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowsTeaching = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SkillType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BaseCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Multiplier = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    RelatedAttribute = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MagicSchool = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ManaCost = table.Column<int>(type: "INTEGER", nullable: true),
                    CooldownSeconds = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    AllowsPassiveAdvancement = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowsTeaching = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesExplodingDice = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxPracticalLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsStartingSkill = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CustomProperties = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillDefinitions_SkillCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalUsagePoints = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    LearnedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastAdvancedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CanTeach = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomProperties = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillUsageLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsageType = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseUsagePoints = table.Column<int>(type: "INTEGER", nullable: false),
                    UsageMultiplier = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: false),
                    FinalUsagePoints = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillLevelBefore = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillLevelAfter = table.Column<int>(type: "INTEGER", nullable: false),
                    DidAdvance = table.Column<bool>(type: "INTEGER", nullable: false),
                    Context = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillUsageLogs_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillUsageLogs_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CurrentRoomId",
                table: "Characters",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId",
                table: "CharacterSkills",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId_SkillDefinitionId",
                table: "CharacterSkills",
                columns: new[] { "CharacterId", "SkillDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CurrentLevel",
                table: "CharacterSkills",
                column: "CurrentLevel");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_LastUsedAt",
                table: "CharacterSkills",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_SkillDefinitionId",
                table: "CharacterSkills",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_DisplayOrder",
                table: "SkillCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_IsActive",
                table: "SkillCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Name",
                table: "SkillCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_CategoryId_SkillType",
                table: "SkillDefinitions",
                columns: new[] { "CategoryId", "SkillType" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_IsActive",
                table: "SkillDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_IsStartingSkill",
                table: "SkillDefinitions",
                column: "IsStartingSkill");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_MagicSchool",
                table: "SkillDefinitions",
                column: "MagicSchool");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_Name",
                table: "SkillDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_CharacterId",
                table: "SkillUsageLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_CharacterId_UsedAt",
                table: "SkillUsageLogs",
                columns: new[] { "CharacterId", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_SkillDefinitionId",
                table: "SkillUsageLogs",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_UsedAt",
                table: "SkillUsageLogs",
                column: "UsedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Rooms_CurrentRoomId",
                table: "Characters",
                column: "CurrentRoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Rooms_CurrentRoomId",
                table: "Characters");

            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.DropTable(
                name: "SkillUsageLogs");

            migrationBuilder.DropTable(
                name: "SkillDefinitions");

            migrationBuilder.DropTable(
                name: "SkillCategories");

            migrationBuilder.DropIndex(
                name: "IX_Characters_CurrentRoomId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentFatigue",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentRoomId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentVitality",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PendingFatigueDamage",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PendingVitalityDamage",
                table: "Characters");
        }
    }
}
