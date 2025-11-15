using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    RolesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreateApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastUpdateApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
