using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmrSubmissionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmrSubmissionError",
                table: "MissionQueues",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubmittedToAmr",
                table: "MissionQueues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedToAmrDate",
                table: "MissionQueues",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmrSubmissionError",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "SubmittedToAmr",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "SubmittedToAmrDate",
                table: "MissionQueues");
        }
    }
}
