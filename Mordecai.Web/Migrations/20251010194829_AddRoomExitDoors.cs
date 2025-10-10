using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomExitDoors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoorName",
                table: "RoomExits",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoorState",
                table: "RoomExits",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoorName",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "DoorState",
                table: "RoomExits");
        }
    }
}
