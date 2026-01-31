using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterEffectsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterEffectDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectType = table.Column<int>(type: "integer", nullable: false),
                    DefaultDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    DefaultIntensity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsStackable = table.Column<bool>(type: "boolean", nullable: false),
                    MaxStacks = table.Column<int>(type: "integer", nullable: false),
                    TickIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsVisibleToOthers = table.Column<bool>(type: "boolean", nullable: false),
                    IconName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EffectColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDispellable = table.Column<bool>(type: "boolean", nullable: false),
                    DispelSkillId = table.Column<int>(type: "integer", nullable: true),
                    DispelDifficulty = table.Column<int>(type: "integer", nullable: false),
                    IsSystemEffect = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEffectDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterEffectImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterEffectDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ImpactType = table.Column<int>(type: "integer", nullable: false),
                    TargetAttribute = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetSkillDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    ModifierValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsPercentage = table.Column<bool>(type: "boolean", nullable: false),
                    DamageType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ScalesWithIntensity = table.Column<bool>(type: "boolean", nullable: false),
                    ApplyOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEffectImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterEffectImpacts_CharacterEffectDefinitions_Character~",
                        column: x => x.CharacterEffectDefinitionId,
                        principalTable: "CharacterEffectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterEffects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    SourceCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceNpcId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceSpellSkillId = table.Column<int>(type: "integer", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTickAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentStacks = table.Column<int>(type: "integer", nullable: false),
                    Intensity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BodyLocation = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovalReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterEffects_CharacterEffectDefinitions_EffectDefinitio~",
                        column: x => x.EffectDefinitionId,
                        principalTable: "CharacterEffectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterEffects_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterEffects_Characters_SourceCharacterId",
                        column: x => x.SourceCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectDefinitions_EffectType",
                table: "CharacterEffectDefinitions",
                column: "EffectType");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectDefinitions_IsActive",
                table: "CharacterEffectDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectDefinitions_IsSystemEffect",
                table: "CharacterEffectDefinitions",
                column: "IsSystemEffect");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectDefinitions_Name",
                table: "CharacterEffectDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectImpacts_CharacterEffectDefinitionId",
                table: "CharacterEffectImpacts",
                column: "CharacterEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectImpacts_ImpactType",
                table: "CharacterEffectImpacts",
                column: "ImpactType");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffectImpacts_TargetSkillDefinitionId",
                table: "CharacterEffectImpacts",
                column: "TargetSkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_CharacterId",
                table: "CharacterEffects",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_CharacterId_EffectDefinitionId_IsActive",
                table: "CharacterEffects",
                columns: new[] { "CharacterId", "EffectDefinitionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_CharacterId_IsActive",
                table: "CharacterEffects",
                columns: new[] { "CharacterId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_EffectDefinitionId",
                table: "CharacterEffects",
                column: "EffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_ExpiresAt",
                table: "CharacterEffects",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_IsActive",
                table: "CharacterEffects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEffects_SourceCharacterId",
                table: "CharacterEffects",
                column: "SourceCharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterEffectImpacts");

            migrationBuilder.DropTable(
                name: "CharacterEffects");

            migrationBuilder.DropTable(
                name: "CharacterEffectDefinitions");
        }
    }
}
