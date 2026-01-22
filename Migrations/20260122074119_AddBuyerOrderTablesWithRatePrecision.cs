using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyerOrderTablesWithRatePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create IPO_GroupMaster table
            migrationBuilder.CreateTable(
                name: "IPO_GroupMaster",
                columns: table => new
                {
                    IPOGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IPOId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_GroupMaster", x => x.IPOGroupId);
                    table.ForeignKey(
                        name: "FK_IPO_GroupMaster_IPO_IPOMaster_IPOId",
                        column: x => x.IPOId,
                        principalTable: "IPO_IPOMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create IPO_BuyerPlaceOrderMaster table
            migrationBuilder.CreateTable(
                name: "IPO_BuyerPlaceOrderMaster",
                columns: table => new
                {
                    BuyerMasterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPOId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_BuyerPlaceOrderMaster", x => x.BuyerMasterId);
                    table.ForeignKey(
                        name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                        column: x => x.GroupId,
                        principalTable: "IPO_GroupMaster",
                        principalColumn: "IPOGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create IPO_BuyerOrder table with proper Rate precision
            migrationBuilder.CreateTable(
                name: "IPO_BuyerOrder",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyerMasterId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    OrderCategory = table.Column<int>(type: "int", nullable: false),
                    InvestorType = table.Column<int>(type: "int", nullable: false),
                    PremiumStrikePrice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    OrderCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_BuyerOrder", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_IPO_BuyerOrder_IPO_BuyerPlaceOrderMaster_BuyerMasterId",
                        column: x => x.BuyerMasterId,
                        principalTable: "IPO_BuyerPlaceOrderMaster",
                        principalColumn: "BuyerMasterId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create IPO_PlaceOrderChild table
            migrationBuilder.CreateTable(
                name: "IPO_PlaceOrderChild",
                columns: table => new
                {
                    POChildId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PANNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllotedQty = table.Column<int>(type: "int", nullable: true),
                    DematNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ChildOrderCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_PlaceOrderChild", x => x.POChildId);
                    table.ForeignKey(
                        name: "FK_IPO_PlaceOrderChild_IPO_BuyerOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "IPO_BuyerOrder",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_IPO_GroupMaster_IPOId",
                table: "IPO_GroupMaster",
                column: "IPOId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_BuyerOrder_BuyerMasterId",
                table: "IPO_BuyerOrder",
                column: "BuyerMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_BuyerPlaceOrderMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_PlaceOrderChild_OrderId",
                table: "IPO_PlaceOrderChild",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IPO_PlaceOrderChild");
            migrationBuilder.DropTable(name: "IPO_BuyerOrder");
            migrationBuilder.DropTable(name: "IPO_BuyerPlaceOrderMaster");
            migrationBuilder.DropTable(name: "IPO_GroupMaster");
        }
    }
}
