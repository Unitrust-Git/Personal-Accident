using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class workerupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LockToken",
                table: "PolicyProvisioningJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "PolicyProvisioningJobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "PolicyProvisioningJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyProvisioningJobs_LockedUntilUtc",
                table: "PolicyProvisioningJobs",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyProvisioningJobs_NextAttemptAt",
                table: "PolicyProvisioningJobs",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyProvisioningJobs_Status",
                table: "PolicyProvisioningJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PolicyProvisioningJobs_LockedUntilUtc",
                table: "PolicyProvisioningJobs");

            migrationBuilder.DropIndex(
                name: "IX_PolicyProvisioningJobs_NextAttemptAt",
                table: "PolicyProvisioningJobs");

            migrationBuilder.DropIndex(
                name: "IX_PolicyProvisioningJobs_Status",
                table: "PolicyProvisioningJobs");

            migrationBuilder.DropColumn(
                name: "LockToken",
                table: "PolicyProvisioningJobs");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "PolicyProvisioningJobs");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "PolicyProvisioningJobs");
        }
    }
}
