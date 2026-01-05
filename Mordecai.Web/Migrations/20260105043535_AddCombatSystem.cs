using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentWounds",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentFatigue",
                table: "ActiveSpawns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVitality",
                table: "ActiveSpawns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentWounds",
                table: "ActiveSpawns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingFatigueDamage",
                table: "ActiveSpawns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingVitalityDamage",
                table: "ActiveSpawns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CombatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EndReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatSessions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CombatParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CombatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveSpawnId = table.Column<int>(type: "integer", nullable: true),
                    ParticipantName = table.Column<string>(type: "text", nullable: false),
                    IsInParryMode = table.Column<bool>(type: "boolean", nullable: false),
                    LastRangedAttack = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TimedPenaltiesJson = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LeaveReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatParticipants_ActiveSpawns_ActiveSpawnId",
                        column: x => x.ActiveSpawnId,
                        principalTable: "ActiveSpawns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombatParticipants_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombatParticipants_CombatSessions_CombatSessionId",
                        column: x => x.CombatSessionId,
                        principalTable: "CombatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombatActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CombatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorParticipantId = table.Column<int>(type: "integer", nullable: false),
                    TargetParticipantId = table.Column<int>(type: "integer", nullable: true),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    AttackRoll = table.Column<int>(type: "integer", nullable: true),
                    DefenseRoll = table.Column<int>(type: "integer", nullable: true),
                    SuccessValue = table.Column<int>(type: "integer", nullable: true),
                    DamageDealt = table.Column<int>(type: "integer", nullable: true),
                    FatigueDamage = table.Column<int>(type: "integer", nullable: true),
                    VitalityDamage = table.Column<int>(type: "integer", nullable: true),
                    WoundsInflicted = table.Column<int>(type: "integer", nullable: true),
                    HitLocation = table.Column<int>(type: "integer", nullable: true),
                    DamageType = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatActionLogs_CombatParticipants_ActorParticipantId",
                        column: x => x.ActorParticipantId,
                        principalTable: "CombatParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombatActionLogs_CombatParticipants_TargetParticipantId",
                        column: x => x.TargetParticipantId,
                        principalTable: "CombatParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombatActionLogs_CombatSessions_CombatSessionId",
                        column: x => x.CombatSessionId,
                        principalTable: "CombatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CombatActionLogs_ActionType",
                table: "CombatActionLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActionLogs_ActorParticipantId",
                table: "CombatActionLogs",
                column: "ActorParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActionLogs_CombatSessionId",
                table: "CombatActionLogs",
                column: "CombatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActionLogs_TargetParticipantId",
                table: "CombatActionLogs",
                column: "TargetParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActionLogs_Timestamp",
                table: "CombatActionLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_ActiveSpawnId",
                table: "CombatParticipants",
                column: "ActiveSpawnId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_CharacterId",
                table: "CombatParticipants",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_CombatSessionId",
                table: "CombatParticipants",
                column: "CombatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_CombatSessionId_IsActive",
                table: "CombatParticipants",
                columns: new[] { "CombatSessionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_IsActive",
                table: "CombatParticipants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CombatSessions_IsActive",
                table: "CombatSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CombatSessions_IsActive_RoomId",
                table: "CombatSessions",
                columns: new[] { "IsActive", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_CombatSessions_RoomId",
                table: "CombatSessions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatSessions_StartedAt",
                table: "CombatSessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombatActionLogs");

            migrationBuilder.DropTable(
                name: "CombatParticipants");

            migrationBuilder.DropTable(
                name: "CombatSessions");

            migrationBuilder.DropColumn(
                name: "CurrentWounds",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentFatigue",
                table: "ActiveSpawns");

            migrationBuilder.DropColumn(
                name: "CurrentVitality",
                table: "ActiveSpawns");

            migrationBuilder.DropColumn(
                name: "CurrentWounds",
                table: "ActiveSpawns");

            migrationBuilder.DropColumn(
                name: "PendingFatigueDamage",
                table: "ActiveSpawns");

            migrationBuilder.DropColumn(
                name: "PendingVitalityDamage",
                table: "ActiveSpawns");
        }
    }
}
