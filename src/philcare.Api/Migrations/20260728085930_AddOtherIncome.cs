using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OtherIncomeId",
                table: "Allocations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OtherIncomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IncomeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountOriginal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FxRateToPhp = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AmountPhp = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReportingYear = table.Column<int>(type: "int", nullable: false),
                    FundCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FundingBucketCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceiptNo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EvidenceLink = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsVoided = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherIncomes_FundingBuckets_FundingBucketCode",
                        column: x => x.FundingBucketCode,
                        principalTable: "FundingBuckets",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OtherIncomes_Funds_FundCode",
                        column: x => x.FundCode,
                        principalTable: "Funds",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_OtherIncomeId",
                table: "Allocations",
                column: "OtherIncomeId");

            migrationBuilder.CreateIndex(
                name: "IX_OtherIncomes_FundCode",
                table: "OtherIncomes",
                column: "FundCode");

            migrationBuilder.CreateIndex(
                name: "IX_OtherIncomes_FundingBucketCode",
                table: "OtherIncomes",
                column: "FundingBucketCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Allocations_OtherIncomes_OtherIncomeId",
                table: "Allocations",
                column: "OtherIncomeId",
                principalTable: "OtherIncomes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allocations_OtherIncomes_OtherIncomeId",
                table: "Allocations");

            migrationBuilder.DropTable(
                name: "OtherIncomes");

            migrationBuilder.DropIndex(
                name: "IX_Allocations_OtherIncomeId",
                table: "Allocations");

            migrationBuilder.DropColumn(
                name: "OtherIncomeId",
                table: "Allocations");
        }
    }
}
