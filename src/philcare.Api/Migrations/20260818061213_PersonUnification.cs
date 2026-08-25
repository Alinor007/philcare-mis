using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace philcare.Api.Migrations
{
    /// <inheritdoc />
    public partial class PersonUnification : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// Data-preserving. EF scaffolded the DropColumns FIRST with no backfill, which would have
        /// destroyed every staff and volunteer name in the database. This hand-written Up widens
        /// Person, creates a Person row for each existing profile, repoints the profiles at it, and
        /// only then drops the migrated columns.
        ///
        /// PersonId is added NULLable although it is logically required: MariaDB 10.4 cannot
        /// MODIFY COLUMN ... NOT NULL additively (the standing project constraint), and the handlers
        /// enforce presence. Same reasoning as Distribution.FundingBucketCode.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. Widen Person with the identity fields the sub-profiles are giving up ------
            migrationBuilder.AddColumn<string>(
                name: "DateOfBirth", table: "GovernancePeople", type: "varchar(50)", maxLength: 50, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Defaults to the enum zero value, not "" — an empty string is not a valid Gender and
            // would throw on materialisation.
            migrationBuilder.AddColumn<string>(
                name: "Gender", table: "GovernancePeople", type: "varchar(20)", maxLength: 20,
                nullable: false, defaultValue: "Unspecified")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CivilStatus", table: "GovernancePeople", type: "varchar(50)", maxLength: 50, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Barangay", table: "GovernancePeople", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City", table: "GovernancePeople", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Province", table: "GovernancePeople", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Region", table: "GovernancePeople", type: "varchar(50)", maxLength: 50, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName", table: "GovernancePeople", type: "varchar(200)", maxLength: 200, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactNumber", table: "GovernancePeople", type: "varchar(50)", maxLength: 50, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl", table: "GovernancePeople", type: "varchar(500)", maxLength: 500, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // ---- 2. Add the links, nullable so existing rows survive the ALTER ----------------
            migrationBuilder.AddColumn<int>(
                name: "PersonId", table: "StaffMembers", type: "int", nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorPersonId", table: "StaffMembers", type: "int", nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonId", table: "Volunteers", type: "int", nullable: true);

            // Correlation key so the backfill can find the Person it just created for each
            // profile. Dropped again at the end of this migration.
            migrationBuilder.Sql("ALTER TABLE `GovernancePeople` ADD COLUMN `_MigrationSourceKey` varchar(32) NULL;");

            // ---- 3. Honour the links that already exist --------------------------------------
            // GovernancePeople.VolunteerId was a real, handler-validated link meaning "this Person
            // IS that Volunteer" — reuse those rows rather than creating a duplicate Person. This
            // is the unification actually happening for records the org had already cross-linked.
            migrationBuilder.Sql(@"
                UPDATE `Volunteers` v
                JOIN `GovernancePeople` p ON p.`VolunteerId` = v.`Id`
                SET v.`PersonId` = p.`Id`
                WHERE v.`PersonId` IS NULL;");

            // StaffId was dead code (no handler ever wrote it), but honour it defensively.
            migrationBuilder.Sql(@"
                UPDATE `StaffMembers` s
                JOIN `GovernancePeople` p ON p.`StaffId` = s.`Id`
                SET s.`PersonId` = p.`Id`
                WHERE s.`PersonId` IS NULL;");

            // Fill gaps on those reused Person rows from the profile they were linked to, without
            // overwriting anything the Person already had.
            migrationBuilder.Sql(@"
                UPDATE `GovernancePeople` p
                JOIN `Volunteers` v ON v.`PersonId` = p.`Id`
                SET p.`Email`         = COALESCE(p.`Email`, v.`Email`),
                    p.`ContactNumber` = COALESCE(p.`ContactNumber`, v.`Phone`),
                    p.`Barangay`      = COALESCE(p.`Barangay`, v.`Barangay`),
                    p.`City`          = COALESCE(p.`City`, v.`City`),
                    p.`Province`      = COALESCE(p.`Province`, v.`Province`),
                    p.`Region`        = COALESCE(p.`Region`, v.`Region`),
                    p.`PhotoUrl`      = COALESCE(p.`PhotoUrl`, v.`PhotoUrl`),
                    p.`Gender`        = CASE WHEN p.`Gender` IN ('', 'Unspecified')
                                             THEN v.`Gender` ELSE p.`Gender` END;");

            // ---- 4. Create a Person for every profile that still has none ---------------------
            migrationBuilder.Sql(@"
                INSERT INTO `GovernancePeople`
                    (`FullName`, `PersonCategory`, `Status`, `Email`, `ContactNumber`, `Gender`,
                     `PhotoUrl`, `DefaultVotingRights`, `IsActive`, `CreatedAt`, `UpdatedAt`,
                     `_MigrationSourceKey`)
                SELECT s.`FullName`, 'MEMBER', 'ACTIVE', s.`Email`, s.`Phone`, 'Unspecified',
                       s.`PhotoUrl`, 0, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6),
                       CONCAT('S:', s.`Id`)
                FROM `StaffMembers` s
                WHERE s.`PersonId` IS NULL;");

            migrationBuilder.Sql(@"
                UPDATE `StaffMembers` s
                JOIN `GovernancePeople` p ON p.`_MigrationSourceKey` = CONCAT('S:', s.`Id`)
                SET s.`PersonId` = p.`Id`
                WHERE s.`PersonId` IS NULL;");

            migrationBuilder.Sql(@"
                INSERT INTO `GovernancePeople`
                    (`FullName`, `PersonCategory`, `Status`, `Email`, `ContactNumber`, `Gender`,
                     `Barangay`, `City`, `Province`, `Region`, `PhotoUrl`,
                     `DefaultVotingRights`, `IsActive`, `CreatedAt`, `UpdatedAt`,
                     `_MigrationSourceKey`)
                SELECT v.`FullName`, 'MEMBER', 'ACTIVE', v.`Email`, v.`Phone`, v.`Gender`,
                       v.`Barangay`, v.`City`, v.`Province`, v.`Region`, v.`PhotoUrl`,
                       0, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), CONCAT('V:', v.`Id`)
                FROM `Volunteers` v
                WHERE v.`PersonId` IS NULL;");

            migrationBuilder.Sql(@"
                UPDATE `Volunteers` v
                JOIN `GovernancePeople` p ON p.`_MigrationSourceKey` = CONCAT('V:', v.`Id`)
                SET v.`PersonId` = p.`Id`
                WHERE v.`PersonId` IS NULL;");

            migrationBuilder.Sql("ALTER TABLE `GovernancePeople` DROP COLUMN `_MigrationSourceKey`;");

            // ---- 5. Only now is it safe to drop the migrated identity columns -----------------
            migrationBuilder.DropIndex(name: "IX_StaffMembers_FullName", table: "StaffMembers");
            migrationBuilder.DropIndex(name: "IX_GovernancePeople_VolunteerId", table: "GovernancePeople");

            migrationBuilder.DropColumn(name: "FullName", table: "StaffMembers");
            migrationBuilder.DropColumn(name: "Email", table: "StaffMembers");
            migrationBuilder.DropColumn(name: "Phone", table: "StaffMembers");
            migrationBuilder.DropColumn(name: "PhotoUrl", table: "StaffMembers");

            migrationBuilder.DropColumn(name: "FullName", table: "Volunteers");
            migrationBuilder.DropColumn(name: "Email", table: "Volunteers");
            migrationBuilder.DropColumn(name: "Phone", table: "Volunteers");
            migrationBuilder.DropColumn(name: "PhotoUrl", table: "Volunteers");
            migrationBuilder.DropColumn(name: "Gender", table: "Volunteers");
            migrationBuilder.DropColumn(name: "Barangay", table: "Volunteers");
            migrationBuilder.DropColumn(name: "City", table: "Volunteers");
            migrationBuilder.DropColumn(name: "Province", table: "Volunteers");
            migrationBuilder.DropColumn(name: "Region", table: "Volunteers");

            migrationBuilder.DropColumn(name: "VolunteerId", table: "GovernancePeople");
            migrationBuilder.DropColumn(name: "StaffId", table: "GovernancePeople");

            // ---- 6. New table, indexes and constraints ---------------------------------------
            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    MembershipNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MembershipType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoinDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RenewalDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExitDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReferredBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memberships_GovernancePeople_PersonId",
                        column: x => x.PersonId,
                        principalTable: "GovernancePeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_PersonId", table: "Volunteers", column: "PersonId", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_PersonId", table: "StaffMembers", column: "PersonId", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_SupervisorPersonId", table: "StaffMembers", column: "SupervisorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernancePeople_FullName", table: "GovernancePeople", column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MembershipNumber", table: "Memberships", column: "MembershipNumber", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_PersonId", table: "Memberships", column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_GovernancePeople_PersonId",
                table: "StaffMembers", column: "PersonId",
                principalTable: "GovernancePeople", principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_GovernancePeople_SupervisorPersonId",
                table: "StaffMembers", column: "SupervisorPersonId",
                principalTable: "GovernancePeople", principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Volunteers_GovernancePeople_PersonId",
                table: "Volunteers", column: "PersonId",
                principalTable: "GovernancePeople", principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <summary>
        /// Structurally reverses Up, but is LOSSY on data: the identity columns come back empty and
        /// merged Person rows cannot be un-merged. Restore from a backup rather than relying on
        /// this if the migration has already run against real data.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_GovernancePeople_PersonId",
                table: "StaffMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_GovernancePeople_SupervisorPersonId",
                table: "StaffMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Volunteers_GovernancePeople_PersonId",
                table: "Volunteers");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Volunteers_PersonId",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_PersonId",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_SupervisorPersonId",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_GovernancePeople_FullName",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "SupervisorPersonId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "City",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "CivilStatus",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "EmergencyContactNumber",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "GovernancePeople");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "GovernancePeople");

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "Volunteers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Volunteers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Volunteers",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Volunteers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Volunteers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Volunteers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Volunteers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Volunteers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Volunteers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "StaffMembers",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "StaffMembers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "StaffMembers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "StaffMembers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "GovernancePeople",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolunteerId",
                table: "GovernancePeople",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_FullName",
                table: "StaffMembers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_GovernancePeople_VolunteerId",
                table: "GovernancePeople",
                column: "VolunteerId");
        }
    }
}
