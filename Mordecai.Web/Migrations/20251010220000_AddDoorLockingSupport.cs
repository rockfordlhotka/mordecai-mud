using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDoorLockingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "RoomExits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LockConfiguration",
                table: "RoomExits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LockDeviceCode",
                table: "RoomExits",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhysicalityTargetValue",
                table: "RoomExits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpellLockCasterId",
                table: "RoomExits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SpellLockAppliedAt",
                table: "RoomExits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpellLockStrength",
                table: "RoomExits",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_LockConfiguration_IsLocked",
                table: "RoomExits",
                columns: new[] { "LockConfiguration", "IsLocked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomExits_LockConfiguration_IsLocked",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "LockConfiguration",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "LockDeviceCode",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "PhysicalityTargetValue",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "SpellLockCasterId",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "SpellLockAppliedAt",
                table: "RoomExits");

            migrationBuilder.DropColumn(
                name: "SpellLockStrength",
                table: "RoomExits");
        }
    }
}
