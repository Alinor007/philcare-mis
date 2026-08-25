using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptNumberingKydAndExpenseApprover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Expenses");

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByPersonId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ApprovedByPersonId",
                table: "Expenses",
                column: "ApprovedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ReceiptNo",
                table: "Donations",
                column: "ReceiptNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_GovernancePeople_ApprovedByPersonId",
                table: "Expenses",
                column: "ApprovedByPersonId",
                principalTable: "GovernancePeople",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_GovernancePeople_ApprovedByPersonId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ApprovedByPersonId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Donations_ReceiptNo",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ApprovedByPersonId",
                table: "Expenses");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Expenses",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
