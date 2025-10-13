using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponAndArmorTemplateProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArmorTemplates",
                columns: table => new
                {
                    ItemTemplateId = table.Column<int>(type: "integer", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    MinimumSkillLevel = table.Column<int>(type: "integer", nullable: false),
                    DamageClass = table.Column<int>(type: "integer", nullable: false),
                    BashingAbsorption = table.Column<int>(type: "integer", nullable: false),
                    CuttingAbsorption = table.Column<int>(type: "integer", nullable: false),
                    PiercingAbsorption = table.Column<int>(type: "integer", nullable: false),
                    ProjectileAbsorption = table.Column<int>(type: "integer", nullable: false),
                    EnergyAbsorption = table.Column<int>(type: "integer", nullable: false),
                    HeatAbsorption = table.Column<int>(type: "integer", nullable: false),
                    ColdAbsorption = table.Column<int>(type: "integer", nullable: false),
                    AcidAbsorption = table.Column<int>(type: "integer", nullable: false),
                    DodgeModifier = table.Column<int>(type: "integer", nullable: false),
                    StrengthModifier = table.Column<int>(type: "integer", nullable: false),
                    HitLocationCoverage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LayerPriority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmorTemplates", x => x.ItemTemplateId);
                    table.ForeignKey(
                        name: "FK_ArmorTemplates_ItemTemplates_ItemTemplateId",
                        column: x => x.ItemTemplateId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArmorTemplates_SkillDefinition_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeaponTemplates",
                columns: table => new
                {
                    ItemTemplateId = table.Column<int>(type: "integer", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    MinimumSkillLevel = table.Column<int>(type: "integer", nullable: false),
                    DamageType = table.Column<int>(type: "integer", nullable: false),
                    DamageClass = table.Column<int>(type: "integer", nullable: false),
                    BaseSuccessValueModifier = table.Column<int>(type: "integer", nullable: false),
                    AttackValueModifier = table.Column<int>(type: "integer", nullable: false),
                    DodgeModifier = table.Column<int>(type: "integer", nullable: false),
                    Range = table.Column<int>(type: "integer", nullable: false),
                    CanKnockback = table.Column<bool>(type: "boolean", nullable: false),
                    IsTwoHanded = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresAmmunition = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponTemplates", x => x.ItemTemplateId);
                    table.ForeignKey(
                        name: "FK_WeaponTemplates_ItemTemplates_ItemTemplateId",
                        column: x => x.ItemTemplateId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeaponTemplates_SkillDefinition_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArmorTemplates_LayerPriority",
                table: "ArmorTemplates",
                column: "LayerPriority");

            migrationBuilder.CreateIndex(
                name: "IX_ArmorTemplates_SkillDefinitionId",
                table: "ArmorTemplates",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponTemplates_SkillDefinitionId",
                table: "WeaponTemplates",
                column: "SkillDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArmorTemplates");

            migrationBuilder.DropTable(
                name: "WeaponTemplates");
        }
    }
}
