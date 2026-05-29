using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZyncMaster.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncStateCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncStates_DeviceId",
                table: "SyncStates");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SyncStates",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_UserId_DeviceId",
                table: "SyncStates",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncStates_UserId_DeviceId",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SyncStates");

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_DeviceId",
                table: "SyncStates",
                column: "DeviceId",
                unique: true);
        }
    }
}
