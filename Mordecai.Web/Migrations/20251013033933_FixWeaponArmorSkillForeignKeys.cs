using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixWeaponArmorSkillForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArmorTemplates_SkillDefinition_SkillDefinitionId",
                table: "ArmorTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_WeaponTemplates_SkillDefinition_SkillDefinitionId",
                table: "WeaponTemplates");

            migrationBuilder.AddForeignKey(
                name: "FK_ArmorTemplates_SkillDefinitions_SkillDefinitionId",
                table: "ArmorTemplates",
                column: "SkillDefinitionId",
                principalTable: "SkillDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WeaponTemplates_SkillDefinitions_SkillDefinitionId",
                table: "WeaponTemplates",
                column: "SkillDefinitionId",
                principalTable: "SkillDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArmorTemplates_SkillDefinitions_SkillDefinitionId",
                table: "ArmorTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_WeaponTemplates_SkillDefinitions_SkillDefinitionId",
                table: "WeaponTemplates");

            migrationBuilder.AddForeignKey(
                name: "FK_ArmorTemplates_SkillDefinition_SkillDefinitionId",
                table: "ArmorTemplates",
                column: "SkillDefinitionId",
                principalTable: "SkillDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WeaponTemplates_SkillDefinition_SkillDefinitionId",
                table: "WeaponTemplates",
                column: "SkillDefinitionId",
                principalTable: "SkillDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
