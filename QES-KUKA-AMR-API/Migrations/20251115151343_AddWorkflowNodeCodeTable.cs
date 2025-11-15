using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowNodeCodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowNodeCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalWorkflowId = table.Column<int>(type: "int", nullable: false),
                    NodeCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNodeCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodeCodes_ExternalWorkflowId",
                table: "WorkflowNodeCodes",
                column: "ExternalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodeCodes_ExternalWorkflowId_NodeCode",
                table: "WorkflowNodeCodes",
                columns: new[] { "ExternalWorkflowId", "NodeCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowNodeCodes");
        }
    }
}
