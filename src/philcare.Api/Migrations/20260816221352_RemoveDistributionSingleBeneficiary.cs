using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDistributionSingleBeneficiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributions_Beneficiaries_BeneficiaryId",
                table: "Distributions");

            migrationBuilder.DropIndex(
                name: "IX_Distributions_BeneficiaryId",
                table: "Distributions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryId",
                table: "Distributions");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "DistributionBeneficiaries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BeneficiaryId",
                table: "Distributions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "DistributionBeneficiaries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_BeneficiaryId",
                table: "Distributions",
                column: "BeneficiaryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_Beneficiaries_BeneficiaryId",
                table: "Distributions",
                column: "BeneficiaryId",
                principalTable: "Beneficiaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
