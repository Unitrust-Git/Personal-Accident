using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedidempootency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceId",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "PaymentTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PaymentTransactions",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
