using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class emailsetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailKind",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailKindDecidedAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailStep_AttemptCount",
                table: "IndividualPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailStep_LastAttemptAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStep_LastError",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailStep_NextRetryAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailStep_Status",
                table: "IndividualPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailStep_SucceededAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailKind",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailKindDecidedAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailStep_AttemptCount",
                table: "CorporatePolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailStep_LastAttemptAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStep_LastError",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailStep_NextRetryAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailStep_Status",
                table: "CorporatePolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailStep_SucceededAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailKind",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailKindDecidedAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_AttemptCount",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_LastAttemptAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_LastError",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_NextRetryAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_Status",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_SucceededAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "EmailKind",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailKindDecidedAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_AttemptCount",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_LastAttemptAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_LastError",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_NextRetryAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_Status",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "EmailStep_SucceededAt",
                table: "CorporatePolicies");
        }
    }
}
