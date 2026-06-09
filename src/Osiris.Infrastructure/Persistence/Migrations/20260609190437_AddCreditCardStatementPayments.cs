using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Osiris.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardStatementPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCardStatementPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardStatementPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardStatementPayments_CreditCardStatements_CreditCard~",
                        column: x => x.CreditCardStatementId,
                        principalTable: "CreditCardStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardStatementPayments_FinancialAccounts_FinancialAcco~",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardStatementPayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatementPayments_CreditCardStatementId",
                table: "CreditCardStatementPayments",
                column: "CreditCardStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatementPayments_FinancialAccountId",
                table: "CreditCardStatementPayments",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatementPayments_TenantId",
                table: "CreditCardStatementPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatementPayments_TenantId_CreditCardStatementId",
                table: "CreditCardStatementPayments",
                columns: new[] { "TenantId", "CreditCardStatementId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatementPayments_TenantId_PaidAt",
                table: "CreditCardStatementPayments",
                columns: new[] { "TenantId", "PaidAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardStatementPayments");
        }
    }
}
