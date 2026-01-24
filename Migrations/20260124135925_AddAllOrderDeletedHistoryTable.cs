using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddAllOrderDeletedHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPO_DeleteOrderHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TotalOrdersDeleted = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_DeleteOrderHistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "Order_DeletedHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    BuyerMasterId = table.Column<int>(type: "int", nullable: false),
                    DeleteHistoryId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    OrderCategory = table.Column<int>(type: "int", nullable: false),
                    InvestorType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_DeletedHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_Order_DeletedHistory_IPO_DeleteOrderHistory_DeleteHistoryId",
                        column: x => x.DeleteHistoryId,
                        principalTable: "IPO_DeleteOrderHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderChild_DeletedHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POChildId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DeleteHistoryId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    PANNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllotedQty = table.Column<int>(type: "int", nullable: true),
                    DematNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChild_DeletedHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_OrderChild_DeletedHistory_IPO_DeleteOrderHistory_DeleteHistoryId",
                        column: x => x.DeleteHistoryId,
                        principalTable: "IPO_DeleteOrderHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderMaster_DeletedHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyerMasterId = table.Column<int>(type: "int", nullable: false),
                    IPOId = table.Column<int>(type: "int", nullable: false),
                    DeleteHistoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderMaster_DeletedHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_OrderMaster_DeletedHistory_IPO_DeleteOrderHistory_DeleteHistoryId",
                        column: x => x.DeleteHistoryId,
                        principalTable: "IPO_DeleteOrderHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_DeletedHistory_DeleteHistoryId",
                table: "Order_DeletedHistory",
                column: "DeleteHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChild_DeletedHistory_DeleteHistoryId",
                table: "OrderChild_DeletedHistory",
                column: "DeleteHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaster_DeletedHistory_DeleteHistoryId",
                table: "OrderMaster_DeletedHistory",
                column: "DeleteHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Order_DeletedHistory");

            migrationBuilder.DropTable(
                name: "OrderChild_DeletedHistory");

            migrationBuilder.DropTable(
                name: "OrderMaster_DeletedHistory");

            migrationBuilder.DropTable(
                name: "IPO_DeleteOrderHistory");
        }
    }
}
