using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AllowsCombat = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowsLogout = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSpecialCommands = table.Column<bool>(type: "INTEGER", nullable: false),
                    HealingRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    SkillLearningBonus = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxOccupancy = table.Column<int>(type: "INTEGER", nullable: false),
                    IsIndoor = table.Column<bool>(type: "INTEGER", nullable: false),
                    EntryMessage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ExitMessage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DifficultyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOutdoor = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeatherType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    NightDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    EntryDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NightEntryDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ExitDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NightExitDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Z = table.Column<int>(type: "INTEGER", nullable: false),
                    OverrideDayNightDescriptions = table.Column<bool>(type: "INTEGER", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomProperties = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rooms_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomExits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromRoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToRoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExitDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NightExitDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    SkillRequired = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillLevelRequired = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomExits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomExits_Rooms_FromRoomId",
                        column: x => x.FromRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomExits_Rooms_ToRoomId",
                        column: x => x.ToRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_FromRoomId_Direction",
                table: "RoomExits",
                columns: new[] { "FromRoomId", "Direction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_IsActive",
                table: "RoomExits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_ToRoomId",
                table: "RoomExits",
                column: "ToRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_IsActive",
                table: "Rooms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomTypeId",
                table: "Rooms",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_X_Y_Z",
                table: "Rooms",
                columns: new[] { "X", "Y", "Z" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_ZoneId",
                table: "Rooms",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_IsActive",
                table: "RoomTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_Name",
                table: "RoomTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zones_IsActive",
                table: "Zones",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Zones_Name",
                table: "Zones",
                column: "Name",
                unique: true);

            // Seed basic room types
            migrationBuilder.InsertData(
                table: "RoomTypes",
                columns: new[] { "Name", "Description", "AllowsCombat", "AllowsLogout", "HasSpecialCommands", "HealingRate", "SkillLearningBonus", "MaxOccupancy", "IsIndoor", "EntryMessage", "ExitMessage", "IsActive" },
                values: new object[,]
                {
                    { "Normal", "A standard room where most activities can take place.", true, true, false, 1.0m, 1.0m, 0, false, null, null, true },
                    { "Safe Room", "A protected area where combat is not allowed and players can safely log out.", false, true, false, 1.5m, 1.0m, 0, false, "You feel a sense of peace and safety here.", "You leave the safety of this area.", true },
                    { "Training Hall", "A dedicated space for practicing skills with enhanced learning.", true, true, true, 1.0m, 1.5m, 0, true, "The air hums with focused energy and determination.", "You leave the training area behind.", true },
                    { "Shop", "A merchant establishment where goods can be bought and sold.", false, true, true, 1.0m, 1.0m, 0, true, "The scent of commerce fills the air.", "You step away from the bustling marketplace.", true },
                    { "Temple", "A sacred place offering healing and spiritual guidance.", false, true, true, 2.0m, 1.0m, 0, true, "A sense of divine presence washes over you.", "You leave the hallowed grounds.", true },
                    { "Inn", "A place of rest and recuperation with enhanced healing.", false, true, true, 2.0m, 1.0m, 0, true, "The warmth and comfort of the inn welcomes you.", "You step out into the world once more.", true },
                    { "Tavern", "A social gathering place where adventurers meet and share tales.", false, true, true, 1.2m, 1.0m, 0, true, "Laughter and conversation fill the air.", "You leave the convivial atmosphere behind.", true },
                    { "Bank", "A secure institution for storing valuables and currency.", false, true, true, 1.0m, 1.0m, 10, true, "The security and order of the bank surrounds you.", "You exit the financial institution.", true },
                    { "Dungeon", "A dangerous underground area filled with threats and treasures.", true, false, false, 0.8m, 1.2m, 0, true, "Darkness and danger lurk in every shadow.", "You emerge from the depths.", true },
                    { "Wilderness", "An outdoor area exposed to the elements and natural dangers.", true, true, false, 0.9m, 1.1m, 0, false, "The wild calls to your adventurous spirit.", "You leave the untamed lands.", true },
                    { "Cave", "A natural underground formation, partially sheltered but still dangerous.", true, true, false, 0.9m, 1.0m, 0, true, "The cool, damp air of the cave surrounds you.", "You step back into the light.", true },
                    { "Tower", "A tall structure offering strategic advantages but exposure to weather.", true, true, false, 1.0m, 1.0m, 0, false, "The height gives you a commanding view.", "You descend from the lofty perch.", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomExits");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "RoomTypes");

            migrationBuilder.DropTable(
                name: "Zones");
        }
    }
}
