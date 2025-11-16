using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionQueueSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RobotIdsJson",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "RobotModelsJson",
                table: "MissionQueueItems");

            migrationBuilder.AlterColumn<string>(
                name: "RequestId",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ContainerCode",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerModelCode",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "MissionQueueItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "IdleNode",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockRobotAfterFinish",
                table: "MissionQueueItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MissionType",
                table: "MissionQueueItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrgId",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotIds",
                table: "MissionQueueItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotModels",
                table: "MissionQueueItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotType",
                table: "MissionQueueItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockMissionCode",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockRobotId",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedUtc",
                table: "MissionQueueItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ViewBoardType",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerCode",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "ContainerModelCode",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "IdleNode",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "LockRobotAfterFinish",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "MissionType",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "RobotIds",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "RobotModels",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "RobotType",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "UnlockMissionCode",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "UnlockRobotId",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "UpdatedUtc",
                table: "MissionQueueItems");

            migrationBuilder.DropColumn(
                name: "ViewBoardType",
                table: "MissionQueueItems");

            migrationBuilder.AlterColumn<string>(
                name: "RequestId",
                table: "MissionQueueItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotIdsJson",
                table: "MissionQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotModelsJson",
                table: "MissionQueueItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
