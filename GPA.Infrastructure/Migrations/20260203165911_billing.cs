using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class billing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PolicyNumber",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BillingStep_AttemptCount",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStep_LastAttemptAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingStep_LastError",
                table: "IndividualPolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStep_NextRetryAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingStep_Status",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStep_SucceededAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebitCreditNoteNumber",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockToken",
                table: "IndividualPolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "IndividualPolicies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStep_AttemptCount",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStep_LastAttemptAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStep_LastError",
                table: "IndividualPolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStep_NextRetryAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStep_Status",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStep_SucceededAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyStep_AttemptCount",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyStep_LastAttemptAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyStep_LastError",
                table: "IndividualPolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyStep_NextRetryAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyStep_Status",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyStep_SucceededAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptStep_AttemptCount",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptStep_LastAttemptAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptStep_LastError",
                table: "IndividualPolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptStep_NextRetryAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptStep_Status",
                table: "IndividualPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptStep_SucceededAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IndividualPolicies",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "IndividualPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PolicyNumber",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BillingStep_AttemptCount",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStep_LastAttemptAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingStep_LastError",
                table: "CorporatePolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStep_NextRetryAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingStep_Status",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStep_SucceededAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebitCreditNoteNumber",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockToken",
                table: "CorporatePolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "CorporatePolicies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStep_AttemptCount",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStep_LastAttemptAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStep_LastError",
                table: "CorporatePolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStep_NextRetryAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStep_Status",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStep_SucceededAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyStep_AttemptCount",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyStep_LastAttemptAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyStep_LastError",
                table: "CorporatePolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyStep_NextRetryAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyStep_Status",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyStep_SucceededAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptStep_AttemptCount",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptStep_LastAttemptAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptStep_LastError",
                table: "CorporatePolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptStep_NextRetryAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptStep_Status",
                table: "CorporatePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptStep_SucceededAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CorporatePolicies",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CorporatePolicies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingStep_AttemptCount",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_LastAttemptAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_LastError",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_NextRetryAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_Status",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_SucceededAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "DebitCreditNoteNumber",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "LockToken",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_AttemptCount",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_LastAttemptAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_LastError",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_NextRetryAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_Status",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_SucceededAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_AttemptCount",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_LastAttemptAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_LastError",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_NextRetryAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_Status",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_SucceededAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_AttemptCount",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_LastAttemptAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_LastError",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_NextRetryAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_Status",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_SucceededAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "IndividualPolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_AttemptCount",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_LastAttemptAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_LastError",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_NextRetryAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_Status",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "BillingStep_SucceededAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "DebitCreditNoteNumber",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "LockToken",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_AttemptCount",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_LastAttemptAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_LastError",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_NextRetryAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_Status",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PaymentStep_SucceededAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_AttemptCount",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_LastAttemptAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_LastError",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_NextRetryAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_Status",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "PolicyStep_SucceededAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_AttemptCount",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_LastAttemptAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_LastError",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_NextRetryAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_Status",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "ReceiptStep_SucceededAt",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CorporatePolicies");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CorporatePolicies");

            migrationBuilder.AlterColumn<string>(
                name: "PolicyNumber",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "IndividualPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PolicyNumber",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "CorporatePolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
