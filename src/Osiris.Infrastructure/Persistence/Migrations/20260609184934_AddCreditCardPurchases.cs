using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Osiris.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCardPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Installments = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardPurchases_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardPurchases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceMonth = table.Column<int>(type: "integer", nullable: false),
                    ReferenceYear = table.Column<int>(type: "integer", nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardStatements_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardStatements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardPurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                    TotalInstallments = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_CreditCardPurchases_CreditCardPurcha~",
                        column: x => x.CreditCardPurchaseId,
                        principalTable: "CreditCardPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_CreditCardStatements_CreditCardState~",
                        column: x => x.CreditCardStatementId,
                        principalTable: "CreditCardStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_CreditCardPurchaseId",
                table: "CreditCardInstallments",
                column: "CreditCardPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_CreditCardStatementId",
                table: "CreditCardInstallments",
                column: "CreditCardStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_TenantId_CreditCardPurchaseId",
                table: "CreditCardInstallments",
                columns: new[] { "TenantId", "CreditCardPurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_TenantId_CreditCardStatementId",
                table: "CreditCardInstallments",
                columns: new[] { "TenantId", "CreditCardStatementId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPurchases_CreditCardId",
                table: "CreditCardPurchases",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPurchases_TenantId_CreditCardId",
                table: "CreditCardPurchases",
                columns: new[] { "TenantId", "CreditCardId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPurchases_TenantId_PurchaseDate",
                table: "CreditCardPurchases",
                columns: new[] { "TenantId", "PurchaseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatements_CreditCardId",
                table: "CreditCardStatements",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatements_TenantId",
                table: "CreditCardStatements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatements_TenantId_CreditCardId_ReferenceYear_Re~",
                table: "CreditCardStatements",
                columns: new[] { "TenantId", "CreditCardId", "ReferenceYear", "ReferenceMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardInstallments");

            migrationBuilder.DropTable(
                name: "CreditCardPurchases");

            migrationBuilder.DropTable(
                name: "CreditCardStatements");
        }
    }
}
