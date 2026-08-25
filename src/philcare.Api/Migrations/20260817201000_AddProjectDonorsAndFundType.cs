using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDonorsAndFundType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_DonorId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_FundCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DonorId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FundCode",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "FundType",
                table: "Projects",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjectDonors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDonors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDonors_Donors_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Donors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDonors_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FundType",
                table: "Projects",
                column: "FundType");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDonors_DonorId",
                table: "ProjectDonors",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDonors_ProjectId_DonorId",
                table: "ProjectDonors",
                columns: new[] { "ProjectId", "DonorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDonors");

            migrationBuilder.DropIndex(
                name: "IX_Projects_FundType",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FundType",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "DonorId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundCode",
                table: "Projects",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DonorId",
                table: "Projects",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FundCode",
                table: "Projects",
                column: "FundCode");
        }
    }
}
