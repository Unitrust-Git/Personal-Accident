using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class correctedcheckouturlbug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "PaymentTransactions");
        }
    }
}
