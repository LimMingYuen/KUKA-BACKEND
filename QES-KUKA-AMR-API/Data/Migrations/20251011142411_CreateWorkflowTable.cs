using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateWorkflowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDiagrams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkflowOuterCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkflowName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkflowModel = table.Column<int>(type: "int", nullable: false),
                    RobotTypeClass = table.Column<int>(type: "int", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ButtonName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreateUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NeedConfirm = table.Column<int>(type: "int", nullable: false),
                    LockRobotAfterFinish = table.Column<int>(type: "int", nullable: false),
                    WorkflowPriority = table.Column<int>(type: "int", nullable: false),
                    TargetAreaCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreSelectedRobotCellCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreSelectedRobotId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDiagrams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDiagrams_WorkflowCode",
                table: "WorkflowDiagrams",
                column: "WorkflowCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDiagrams");
        }
    }
}
