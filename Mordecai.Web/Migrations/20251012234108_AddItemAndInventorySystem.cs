using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAndInventorySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterInventories",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxVolume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LastCalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterInventories", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_CharacterInventories_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    WeaponType = table.Column<int>(type: "integer", nullable: true),
                    ArmorSlot = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    IsStackable = table.Column<bool>(type: "boolean", nullable: false),
                    MaxStackSize = table.Column<int>(type: "integer", nullable: false),
                    IsDroppable = table.Column<bool>(type: "boolean", nullable: false),
                    IsTradeable = table.Column<bool>(type: "boolean", nullable: false),
                    BindOnPickup = table.Column<bool>(type: "boolean", nullable: false),
                    BindOnEquip = table.Column<bool>(type: "boolean", nullable: false),
                    IsContainer = table.Column<bool>(type: "boolean", nullable: false),
                    ContainerMaxWeight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ContainerMaxVolume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ContainerAllowedTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContainerWeightReduction = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    ContainerVolumeReduction = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    HasDurability = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDurability = table.Column<int>(type: "integer", nullable: true),
                    ConsumableValue = table.Column<int>(type: "integer", nullable: true),
                    MagicLevel = table.Column<int>(type: "integer", nullable: true),
                    Rarity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CustomProperties = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemAttributeModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemTemplateId = table.Column<int>(type: "integer", nullable: false),
                    AttributeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModifierType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModifierValue = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemAttributeModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemAttributeModifiers_ItemTemplates_ItemTemplateId",
                        column: x => x.ItemTemplateId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemTemplateId = table.Column<int>(type: "integer", nullable: false),
                    CurrentRoomId = table.Column<int>(type: "integer", nullable: true),
                    OwnerCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContainerItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquippedSlot = table.Column<int>(type: "integer", nullable: true),
                    StackSize = table.Column<int>(type: "integer", nullable: false),
                    CurrentDurability = table.Column<int>(type: "integer", nullable: true),
                    IsEquipped = table.Column<bool>(type: "boolean", nullable: false),
                    IsBound = table.Column<bool>(type: "boolean", nullable: false),
                    CustomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PickedUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomProperties = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Characters_OwnerCharacterId",
                        column: x => x.OwnerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_ItemTemplates_ItemTemplateId",
                        column: x => x.ItemTemplateId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_Items_ContainerItemId",
                        column: x => x.ContainerItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_Rooms_CurrentRoomId",
                        column: x => x.CurrentRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemSkillBonuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemTemplateId = table.Column<int>(type: "integer", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    BonusType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BonusValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSkillBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSkillBonuses_ItemTemplates_ItemTemplateId",
                        column: x => x.ItemTemplateId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemSkillBonuses_SkillDefinition_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventories_LastCalculatedAt",
                table: "CharacterInventories",
                column: "LastCalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttributeModifiers_AttributeName",
                table: "ItemAttributeModifiers",
                column: "AttributeName");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttributeModifiers_ItemTemplateId",
                table: "ItemAttributeModifiers",
                column: "ItemTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttributeModifiers_ItemTemplateId_AttributeName",
                table: "ItemAttributeModifiers",
                columns: new[] { "ItemTemplateId", "AttributeName" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ContainerItemId",
                table: "Items",
                column: "ContainerItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedAt",
                table: "Items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CurrentRoomId",
                table: "Items",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemTemplateId",
                table: "Items",
                column: "ItemTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OwnerCharacterId",
                table: "Items",
                column: "OwnerCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OwnerCharacterId_IsEquipped",
                table: "Items",
                columns: new[] { "OwnerCharacterId", "IsEquipped" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSkillBonuses_ItemTemplateId",
                table: "ItemSkillBonuses",
                column: "ItemTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSkillBonuses_ItemTemplateId_SkillDefinitionId",
                table: "ItemSkillBonuses",
                columns: new[] { "ItemTemplateId", "SkillDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSkillBonuses_SkillDefinitionId",
                table: "ItemSkillBonuses",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTemplates_IsActive",
                table: "ItemTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTemplates_IsContainer",
                table: "ItemTemplates",
                column: "IsContainer");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTemplates_ItemType",
                table: "ItemTemplates",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTemplates_Name",
                table: "ItemTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTemplates_Rarity",
                table: "ItemTemplates",
                column: "Rarity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterInventories");

            migrationBuilder.DropTable(
                name: "ItemAttributeModifiers");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ItemSkillBonuses");

            migrationBuilder.DropTable(
                name: "ItemTemplates");
        }
    }
}
