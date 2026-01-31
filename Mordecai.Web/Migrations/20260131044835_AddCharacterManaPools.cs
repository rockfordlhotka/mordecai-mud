using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterManaPools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterManaPools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    School = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentMana = table.Column<int>(type: "integer", nullable: false),
                    MaxMana = table.Column<int>(type: "integer", nullable: false),
                    LastRegenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GatheringStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterManaPools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterManaPools_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterManaPools_CharacterId_School",
                table: "CharacterManaPools",
                columns: new[] { "CharacterId", "School" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterManaPools");
        }
    }
}
