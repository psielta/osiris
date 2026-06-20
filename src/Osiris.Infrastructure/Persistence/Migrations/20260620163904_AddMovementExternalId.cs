using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Osiris.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "FinancialAccountMovements",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccountMovements_TenantId_FinancialAccountId_Exter~",
                table: "FinancialAccountMovements",
                columns: new[] { "TenantId", "FinancialAccountId", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialAccountMovements_TenantId_FinancialAccountId_Exter~",
                table: "FinancialAccountMovements");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "FinancialAccountMovements");
        }
    }
}
