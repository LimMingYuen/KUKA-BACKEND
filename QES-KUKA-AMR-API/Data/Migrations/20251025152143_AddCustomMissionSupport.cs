using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomMissionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionQueues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowId",
                table: "MissionQueues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowCode",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ContainerCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerModelCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdleNode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockRobotAfterFinish",
                table: "MissionQueues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MissionDataJson",
                table: "MissionQueues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionType",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotIds",
                table: "MissionQueues",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotModels",
                table: "MissionQueues",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotType",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockMissionCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockRobotId",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewBoardType",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ContainerModelCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "IdleNode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "LockRobotAfterFinish",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "MissionDataJson",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "MissionType",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotIds",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotModels",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotType",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "UnlockMissionCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "UnlockRobotId",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ViewBoardType",
                table: "MissionQueues");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionQueues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowId",
                table: "MissionQueues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowCode",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
