using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowZoneMappingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowZoneMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalWorkflowId = table.Column<int>(type: "int", nullable: false),
                    WorkflowCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkflowName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ZoneCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MatchedNodesCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowZoneMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowZoneMappings_ExternalWorkflowId",
                table: "WorkflowZoneMappings",
                column: "ExternalWorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowZoneMappings_ZoneCode",
                table: "WorkflowZoneMappings",
                column: "ZoneCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowZoneMappings");
        }
    }
}
