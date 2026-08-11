using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramBeneficiaryWorkflowRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Projects",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConcurrencyStamp",
                table: "FundingBuckets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BeneficiaryCount",
                table: "Distributions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ExpenseId",
                table: "Distributions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitValuePhp",
                table: "Distributions",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ActivityParticipants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualBeneficiaries",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "Activities",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_FullName",
                table: "Participants",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_ParticipantType_Status",
                table: "Participants",
                columns: new[] { "ParticipantType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_DistributionDate",
                table: "Distributions",
                column: "DistributionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_ExpenseId",
                table: "Distributions",
                column: "ExpenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_Expenses_ExpenseId",
                table: "Distributions",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributions_Expenses_ExpenseId",
                table: "Distributions");

            migrationBuilder.DropIndex(
                name: "IX_Participants_FullName",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Participants_ParticipantType_Status",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Distributions_DistributionDate",
                table: "Distributions");

            migrationBuilder.DropIndex(
                name: "IX_Distributions_ExpenseId",
                table: "Distributions");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "FundingBuckets");

            migrationBuilder.DropColumn(
                name: "BeneficiaryCount",
                table: "Distributions");

            migrationBuilder.DropColumn(
                name: "ExpenseId",
                table: "Distributions");

            migrationBuilder.DropColumn(
                name: "UnitValuePhp",
                table: "Distributions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ActivityParticipants");

            migrationBuilder.DropColumn(
                name: "ActualBeneficiaries",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ActualEndDate",
                table: "Activities");
        }
    }
}
