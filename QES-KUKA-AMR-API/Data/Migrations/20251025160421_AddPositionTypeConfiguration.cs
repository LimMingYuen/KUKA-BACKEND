using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionTypeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PositionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PositionTypes_ActualValue",
                table: "PositionTypes",
                column: "ActualValue",
                unique: true);

            // Seed initial position types
            migrationBuilder.InsertData(
                table: "PositionTypes",
                columns: new[] { "DisplayName", "ActualValue", "Description", "IsActive", "CreatedUtc", "UpdatedUtc" },
                values: new object[,]
                {
                    { "Node Point", "NODE_POINT", "Standard navigation node point", true, DateTime.UtcNow, DateTime.UtcNow },
                    { "Area", "AREA", "Designated area or zone", true, DateTime.UtcNow, DateTime.UtcNow },
                    { "Charging Point", "CHARGING_POINT", "Robot charging station", true, DateTime.UtcNow, DateTime.UtcNow },
                    { "Parking Point", "PARKING_POINT", "Robot parking location", true, DateTime.UtcNow, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PositionTypes");
        }
    }
}
