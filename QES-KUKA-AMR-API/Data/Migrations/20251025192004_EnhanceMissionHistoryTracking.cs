using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceMissionHistoryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionHistories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "MissionHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MissionHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "MissionHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionType",
                table: "MissionHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "MissionHistories",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "MissionType",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "MissionHistories");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionHistories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
