using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSpawnerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NpcTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Strength = table.Column<int>(type: "integer", nullable: false),
                    Endurance = table.Column<int>(type: "integer", nullable: false),
                    Coordination = table.Column<int>(type: "integer", nullable: false),
                    Quickness = table.Column<int>(type: "integer", nullable: false),
                    Intelligence = table.Column<int>(type: "integer", nullable: false),
                    Willpower = table.Column<int>(type: "integer", nullable: false),
                    Charisma = table.Column<int>(type: "integer", nullable: false),
                    IsHostile = table.Column<bool>(type: "boolean", nullable: false),
                    IsGroupAssist = table.Column<bool>(type: "boolean", nullable: false),
                    CanWander = table.Column<bool>(type: "boolean", nullable: false),
                    BehaviorConfig = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LootConfig = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpawnerTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SpawnBehavior = table.Column<int>(type: "integer", nullable: false),
                    SpawnIntervalMin = table.Column<int>(type: "integer", nullable: false),
                    SpawnIntervalMax = table.Column<int>(type: "integer", nullable: false),
                    MaxActiveCreatures = table.Column<int>(type: "integer", nullable: false),
                    RespawnOnDeath = table.Column<bool>(type: "boolean", nullable: false),
                    ConditionsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnerTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpawnerInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpawnerTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    ZoneId = table.Column<int>(type: "integer", nullable: true),
                    CurrentRoomId = table.Column<int>(type: "integer", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSpawnTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextSpawnTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnerInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnerInstances_Rooms_CurrentRoomId",
                        column: x => x.CurrentRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SpawnerInstances_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpawnerInstances_SpawnerTemplates_SpawnerTemplateId",
                        column: x => x.SpawnerTemplateId,
                        principalTable: "SpawnerTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpawnerInstances_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpawnerNpcEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpawnerTemplateId = table.Column<int>(type: "integer", nullable: false),
                    NpcTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    MinLevel = table.Column<int>(type: "integer", nullable: false),
                    MaxLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnerNpcEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnerNpcEntries_NpcTemplates_NpcTemplateId",
                        column: x => x.NpcTemplateId,
                        principalTable: "NpcTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpawnerNpcEntries_SpawnerTemplates_SpawnerTemplateId",
                        column: x => x.SpawnerTemplateId,
                        principalTable: "SpawnerTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActiveSpawns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NpcId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpawnerInstanceId = table.Column<int>(type: "integer", nullable: false),
                    NpcTemplateId = table.Column<int>(type: "integer", nullable: false),
                    SpawnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentRoomId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DespawnReason = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveSpawns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActiveSpawns_NpcTemplates_NpcTemplateId",
                        column: x => x.NpcTemplateId,
                        principalTable: "NpcTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActiveSpawns_Rooms_CurrentRoomId",
                        column: x => x.CurrentRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActiveSpawns_SpawnerInstances_SpawnerInstanceId",
                        column: x => x.SpawnerInstanceId,
                        principalTable: "SpawnerInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_CurrentRoomId",
                table: "ActiveSpawns",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_IsActive",
                table: "ActiveSpawns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_NpcId",
                table: "ActiveSpawns",
                column: "NpcId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_NpcTemplateId",
                table: "ActiveSpawns",
                column: "NpcTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_SpawnedAt",
                table: "ActiveSpawns",
                column: "SpawnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_SpawnerInstanceId",
                table: "ActiveSpawns",
                column: "SpawnerInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSpawns_SpawnerInstanceId_IsActive",
                table: "ActiveSpawns",
                columns: new[] { "SpawnerInstanceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NpcTemplates_IsActive",
                table: "NpcTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NpcTemplates_IsHostile",
                table: "NpcTemplates",
                column: "IsHostile");

            migrationBuilder.CreateIndex(
                name: "IX_NpcTemplates_Level",
                table: "NpcTemplates",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_NpcTemplates_Name",
                table: "NpcTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_CurrentRoomId",
                table: "SpawnerInstances",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_IsEnabled",
                table: "SpawnerInstances",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_NextSpawnTime",
                table: "SpawnerInstances",
                column: "NextSpawnTime");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_RoomId",
                table: "SpawnerInstances",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_SpawnerTemplateId",
                table: "SpawnerInstances",
                column: "SpawnerTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_Type_IsEnabled",
                table: "SpawnerInstances",
                columns: new[] { "Type", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerInstances_ZoneId",
                table: "SpawnerInstances",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerNpcEntries_NpcTemplateId",
                table: "SpawnerNpcEntries",
                column: "NpcTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerNpcEntries_SpawnerTemplateId",
                table: "SpawnerNpcEntries",
                column: "SpawnerTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerNpcEntries_SpawnerTemplateId_NpcTemplateId",
                table: "SpawnerNpcEntries",
                columns: new[] { "SpawnerTemplateId", "NpcTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerTemplates_IsActive",
                table: "SpawnerTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerTemplates_Name",
                table: "SpawnerTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnerTemplates_SpawnBehavior",
                table: "SpawnerTemplates",
                column: "SpawnBehavior");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveSpawns");

            migrationBuilder.DropTable(
                name: "SpawnerNpcEntries");

            migrationBuilder.DropTable(
                name: "SpawnerInstances");

            migrationBuilder.DropTable(
                name: "NpcTemplates");

            migrationBuilder.DropTable(
                name: "SpawnerTemplates");
        }
    }
}
