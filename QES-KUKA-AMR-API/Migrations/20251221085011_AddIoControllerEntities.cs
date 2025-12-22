using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddIoControllerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IoControllerDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PollingIntervalMs = table.Column<int>(type: "int", nullable: false),
                    ConnectionTimeoutMs = table.Column<int>(type: "int", nullable: false),
                    LastPollUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastConnectionSuccess = table.Column<bool>(type: "bit", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoControllerDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IoChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ChannelNumber = table.Column<int>(type: "int", nullable: false),
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentState = table.Column<bool>(type: "bit", nullable: false),
                    FailSafeValue = table.Column<bool>(type: "bit", nullable: true),
                    FsvEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastStateChangeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IoChannels_IoControllerDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "IoControllerDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IoStateLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ChannelNumber = table.Column<int>(type: "int", nullable: false),
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    PreviousState = table.Column<bool>(type: "bit", nullable: false),
                    NewState = table.Column<bool>(type: "bit", nullable: false),
                    ChangeSource = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChangedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoStateLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IoStateLogs_IoControllerDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "IoControllerDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IoChannels_DeviceId",
                table: "IoChannels",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_IoChannels_DeviceId_ChannelType_ChannelNumber",
                table: "IoChannels",
                columns: new[] { "DeviceId", "ChannelType", "ChannelNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IoControllerDevices_DeviceName",
                table: "IoControllerDevices",
                column: "DeviceName");

            migrationBuilder.CreateIndex(
                name: "IX_IoControllerDevices_IpAddress_Port",
                table: "IoControllerDevices",
                columns: new[] { "IpAddress", "Port" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IoControllerDevices_IsActive",
                table: "IoControllerDevices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IoStateLogs_ChangedUtc",
                table: "IoStateLogs",
                column: "ChangedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IoStateLogs_DeviceId",
                table: "IoStateLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_IoStateLogs_DeviceId_ChangedUtc",
                table: "IoStateLogs",
                columns: new[] { "DeviceId", "ChangedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IoStateLogs_DeviceId_ChannelNumber_ChannelType",
                table: "IoStateLogs",
                columns: new[] { "DeviceId", "ChannelNumber", "ChannelType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IoChannels");

            migrationBuilder.DropTable(
                name: "IoStateLogs");

            migrationBuilder.DropTable(
                name: "IoControllerDevices");
        }
    }
}
