using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddIPOAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPO_Analysis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPOId = table.Column<int>(type: "int", nullable: false),
                    AnalysisType = table.Column<int>(type: "int", nullable: false),
                    ExpectedApplications_Retail = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ExpectedApplications_SHNI = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ExpectedApplications_BHNI = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ActualAllottedQty_Total = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ActualAllottedQty_Retail = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ActualAllottedQty_SHNI = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ActualAllottedQty_BHNI = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ProfitMargin = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SpotPremium = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SpotPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CalculatedResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_Analysis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IPO_Analysis_IPO_IPOMaster_IPOId",
                        column: x => x.IPOId,
                        principalTable: "IPO_IPOMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IPO_Analysis_IPOId_AnalysisType_CompanyId",
                table: "IPO_Analysis",
                columns: new[] { "IPOId", "AnalysisType", "CompanyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPO_Analysis");
        }
    }
}
