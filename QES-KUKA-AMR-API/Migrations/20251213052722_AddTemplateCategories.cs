using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "SavedCustomMissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TemplateCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCustomMissions_CategoryId",
                table: "SavedCustomMissions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategories_Name",
                table: "TemplateCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedCustomMissions_TemplateCategories_CategoryId",
                table: "SavedCustomMissions",
                column: "CategoryId",
                principalTable: "TemplateCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedCustomMissions_TemplateCategories_CategoryId",
                table: "SavedCustomMissions");

            migrationBuilder.DropTable(
                name: "TemplateCategories");

            migrationBuilder.DropIndex(
                name: "IX_SavedCustomMissions_CategoryId",
                table: "SavedCustomMissions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "SavedCustomMissions");
        }
    }
}
