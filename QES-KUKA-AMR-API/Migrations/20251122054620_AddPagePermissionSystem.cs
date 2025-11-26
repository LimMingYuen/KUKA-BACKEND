using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddPagePermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapCodeQueueConfigurations");

            migrationBuilder.DropTable(
                name: "QueueItems");

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PageIcon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PagePath",
                table: "Pages",
                column: "PagePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PageId",
                table: "RolePermissions",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PageId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PageId",
                table: "UserPermissions",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_PageId",
                table: "UserPermissions",
                columns: new[] { "UserId", "PageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.CreateTable(
                name: "MapCodeQueueConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DefaultPriority = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MaxConcurrentTasks = table.Column<int>(type: "int", nullable: false),
                    MaxQueueDepth = table.Column<int>(type: "int", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapCodeQueueConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedRobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MissionDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrgId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProcessingStartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueuePosition = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiresOptimization = table.Column<bool>(type: "bit", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    RobotIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SavedMissionId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetNodeCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetX = table.Column<double>(type: "float", nullable: true),
                    TargetY = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapCodeQueueConfig_MapCode",
                table: "MapCodeQueueConfigurations",
                column: "MapCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_MapProcessing",
                table: "QueueItems",
                columns: new[] { "MapCode", "Status", "Priority", "QueuePosition" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_MapStatus",
                table: "QueueItems",
                columns: new[] { "MapCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_MissionCode",
                table: "QueueItems",
                column: "MissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_RobotStatus",
                table: "QueueItems",
                columns: new[] { "AssignedRobotId", "Status" });
        }
    }
}
