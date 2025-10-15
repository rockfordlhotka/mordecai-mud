using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CopperCoins",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GoldCoins",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlatinumCoins",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SilverCoins",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CopperCoins",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "GoldCoins",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PlatinumCoins",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SilverCoins",
                table: "Characters");
        }
    }
}
