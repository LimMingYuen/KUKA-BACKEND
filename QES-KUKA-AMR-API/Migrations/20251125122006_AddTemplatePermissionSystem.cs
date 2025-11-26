using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplatePermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleTemplatePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    SavedCustomMissionId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTemplatePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleTemplatePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleTemplatePermissions_SavedCustomMissions_SavedCustomMissionId",
                        column: x => x.SavedCustomMissionId,
                        principalTable: "SavedCustomMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTemplatePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SavedCustomMissionId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTemplatePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTemplatePermissions_SavedCustomMissions_SavedCustomMissionId",
                        column: x => x.SavedCustomMissionId,
                        principalTable: "SavedCustomMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTemplatePermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplatePermissions_RoleId",
                table: "RoleTemplatePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplatePermissions_RoleId_SavedCustomMissionId",
                table: "RoleTemplatePermissions",
                columns: new[] { "RoleId", "SavedCustomMissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplatePermissions_SavedCustomMissionId",
                table: "RoleTemplatePermissions",
                column: "SavedCustomMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTemplatePermissions_SavedCustomMissionId",
                table: "UserTemplatePermissions",
                column: "SavedCustomMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTemplatePermissions_UserId",
                table: "UserTemplatePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTemplatePermissions_UserId_SavedCustomMissionId",
                table: "UserTemplatePermissions",
                columns: new[] { "UserId", "SavedCustomMissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleTemplatePermissions");

            migrationBuilder.DropTable(
                name: "UserTemplatePermissions");
        }
    }
}
