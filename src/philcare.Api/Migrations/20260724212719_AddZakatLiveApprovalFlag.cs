using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddZakatLiveApprovalFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLiveApproval",
                table: "ZakatEligibilities",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZakatEligibilities_ParticipantId_IsLiveApproval",
                table: "ZakatEligibilities",
                columns: new[] { "ParticipantId", "IsLiveApproval" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZakatEligibilities_ParticipantId_IsLiveApproval",
                table: "ZakatEligibilities");

            migrationBuilder.DropColumn(
                name: "IsLiveApproval",
                table: "ZakatEligibilities");
        }
    }
}
