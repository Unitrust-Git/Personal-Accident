using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class worker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IndividualPolicies_PaymentTransactionId",
                table: "IndividualPolicies");

            migrationBuilder.DropIndex(
                name: "IX_CorporatePolicies_PaymentTransactionId",
                table: "CorporatePolicies");

            migrationBuilder.CreateTable(
                name: "PolicyProvisioningJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyProvisioningJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyProvisioningJobs_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndividualPolicies_PaymentTransactionId",
                table: "IndividualPolicies",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorporatePolicies_PaymentTransactionId",
                table: "CorporatePolicies",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyProvisioningJobs_PaymentTransactionId",
                table: "PolicyProvisioningJobs",
                column: "PaymentTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyProvisioningJobs");

            migrationBuilder.DropIndex(
                name: "IX_IndividualPolicies_PaymentTransactionId",
                table: "IndividualPolicies");

            migrationBuilder.DropIndex(
                name: "IX_CorporatePolicies_PaymentTransactionId",
                table: "CorporatePolicies");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualPolicies_PaymentTransactionId",
                table: "IndividualPolicies",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporatePolicies_PaymentTransactionId",
                table: "CorporatePolicies",
                column: "PaymentTransactionId");
        }
    }
}
