using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomEffectsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create RoomEffectDefinitions table
            migrationBuilder.CreateTable(
                name: "RoomEffectDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EffectType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: ""),
                    IconName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EffectColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DetectionSkillId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetectionDifficulty = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 0),
                    DefaultDuration = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    DefaultIntensity = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 1.0m),
                    IsStackable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MaxStacks = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    TickInterval = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RemovalMethods = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, defaultValue: "[\"time\"]"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectDefinitions", x => x.Id);
                });

            // Create RoomEffectImpacts table
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
                    ImpactValue = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 0),
                    ImpactFormula = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsPercentage = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
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

            // Create RoomEffects table
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
                    StackCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Intensity = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 1.0m),
                    StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastTickTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CustomData = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
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

            // Create RoomEffectApplicationLogs table
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
                    ImpactValue = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 0),
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

            // Create indexes for RoomEffectDefinitions
            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_Name",
                table: "RoomEffectDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_IsActive",
                table: "RoomEffectDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_EffectType",
                table: "RoomEffectDefinitions",
                column: "EffectType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_Category",
                table: "RoomEffectDefinitions",
                column: "Category");

            // Create indexes for RoomEffectImpacts
            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_RoomEffectDefinitionId",
                table: "RoomEffectImpacts",
                column: "RoomEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_ImpactType",
                table: "RoomEffectImpacts",
                column: "ImpactType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_TargetType",
                table: "RoomEffectImpacts",
                column: "TargetType");

            // Create indexes for RoomEffects
            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_RoomId",
                table: "RoomEffects",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_RoomEffectDefinitionId",
                table: "RoomEffects",
                column: "RoomEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_EndTime",
                table: "RoomEffects",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_IsActive",
                table: "RoomEffects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_CasterCharacterId",
                table: "RoomEffects",
                column: "CasterCharacterId");

            // Create indexes for RoomEffectApplicationLogs
            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_RoomEffectId",
                table: "RoomEffectApplicationLogs",
                column: "RoomEffectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_CharacterId",
                table: "RoomEffectApplicationLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_Timestamp",
                table: "RoomEffectApplicationLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_ApplicationType",
                table: "RoomEffectApplicationLogs",
                column: "ApplicationType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse dependency order
            migrationBuilder.DropTable(
                name: "RoomEffectApplicationLogs");

            migrationBuilder.DropTable(
                name: "RoomEffectImpacts");

            migrationBuilder.DropTable(
                name: "RoomEffects");

            migrationBuilder.DropTable(
                name: "RoomEffectDefinitions");
        }
    }
}