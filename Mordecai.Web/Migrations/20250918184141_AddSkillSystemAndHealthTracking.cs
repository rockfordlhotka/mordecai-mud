using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillSystemAndHealthTracking : Migration
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
                name: "RoomEffectDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EffectType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IconName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EffectColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetectionSkillId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetectionDifficulty = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultIntensity = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsStackable = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxStacks = table.Column<int>(type: "INTEGER", nullable: false),
                    TickInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    RemovalMethods = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SkillType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RelatedAttribute = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    MagicSchool = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    IsStartingSkill = table.Column<bool>(type: "INTEGER", nullable: false),
                    BaseExperienceRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    LevelMultiplier = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffectImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomEffectDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpactType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TargetSkillId = table.Column<int>(type: "INTEGER", nullable: true),
                    TargetAttribute = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    ImpactValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    ImpactFormula = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsPercentage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DamageType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    ResistanceSkillId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEffectImpacts_RoomEffectDefinitions_RoomEffectDefinitionId",
                        column: x => x.RoomEffectDefinitionId,
                        principalTable: "RoomEffectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomEffectDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SourceId = table.Column<string>(type: "TEXT", nullable: true),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CasterCharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StackCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Intensity = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastTickTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CustomData = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEffects_RoomEffectDefinitions_RoomEffectDefinitionId",
                        column: x => x.RoomEffectDefinitionId,
                        principalTable: "RoomEffectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomEffects_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LearnedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffectApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomEffectId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApplicationType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ImpactType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ImpactValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    ResistanceRoll = table.Column<decimal>(type: "TEXT", nullable: true),
                    ResistanceSuccess = table.Column<bool>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectApplicationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEffectApplicationLogs_RoomEffects_RoomEffectId",
                        column: x => x.RoomEffectId,
                        principalTable: "RoomEffects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_CharacterSkills_LastUsedAt",
                table: "CharacterSkills",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_Level",
                table: "CharacterSkills",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_SkillDefinitionId",
                table: "CharacterSkills",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_ApplicationType",
                table: "RoomEffectApplicationLogs",
                column: "ApplicationType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_CharacterId",
                table: "RoomEffectApplicationLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_RoomEffectId",
                table: "RoomEffectApplicationLogs",
                column: "RoomEffectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_Timestamp",
                table: "RoomEffectApplicationLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_Category",
                table: "RoomEffectDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_EffectType",
                table: "RoomEffectDefinitions",
                column: "EffectType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_IsActive",
                table: "RoomEffectDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_Name",
                table: "RoomEffectDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_ImpactType",
                table: "RoomEffectImpacts",
                column: "ImpactType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_RoomEffectDefinitionId",
                table: "RoomEffectImpacts",
                column: "RoomEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_TargetType",
                table: "RoomEffectImpacts",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_CasterCharacterId",
                table: "RoomEffects",
                column: "CasterCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_EndTime",
                table: "RoomEffects",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_IsActive",
                table: "RoomEffects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_RoomEffectDefinitionId",
                table: "RoomEffects",
                column: "RoomEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_RoomId",
                table: "RoomEffects",
                column: "RoomId");

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
                name: "IX_SkillDefinitions_RelatedAttribute",
                table: "SkillDefinitions",
                column: "RelatedAttribute");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_SkillType",
                table: "SkillDefinitions",
                column: "SkillType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.DropTable(
                name: "RoomEffectApplicationLogs");

            migrationBuilder.DropTable(
                name: "RoomEffectImpacts");

            migrationBuilder.DropTable(
                name: "SkillDefinitions");

            migrationBuilder.DropTable(
                name: "RoomEffects");

            migrationBuilder.DropTable(
                name: "RoomEffectDefinitions");

            migrationBuilder.DropColumn(
                name: "CurrentFatigue",
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
