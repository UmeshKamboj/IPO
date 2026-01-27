using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPO_ApiLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QueryString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RequestTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_ApiLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IPO_BuyerPlaceOrderMaster",
                columns: table => new
                {
                    BuyerMasterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPOId = table.Column<int>(type: "int", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "IPO_ClientDeleteHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TotalClientsDeleted = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_ClientDeleteHistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "IPO_TypeMaster",
                columns: table => new
                {
                    IPOTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPOTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_TypeMaster", x => x.IPOTypeID);
                });

            migrationBuilder.CreateTable(
                name: "IPO_UserMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_UserMasters", x => x.Id);
                });

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
                    ApplicateRate = table.Column<bool>(type: "bit", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "IPO_ClientDeleteHistoryDetail",
                columns: table => new
                {
                    DetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HistoryId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    PANNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ClientDPId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_ClientDeleteHistoryDetail", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_IPO_ClientDeleteHistoryDetail_IPO_ClientDeleteHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "IPO_ClientDeleteHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IPO_IPOMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPOName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPOType = table.Column<int>(type: "int", nullable: true),
                    IPO_Upper_Price_Band = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OpenIPOPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Total_IPO_Size_Cr = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IPO_Retail_Lot_Size = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IPO_SHNI_Lot_Size = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IPO_BHNI_Lot_Size = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Retail_Percentage = table.Column<int>(type: "int", nullable: false),
                    BHNI_Percentage = table.Column<int>(type: "int", nullable: true),
                    SHNI_Percentage = table.Column<int>(type: "int", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_IPOMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IPO_IPOMaster_IPO_TypeMaster_IPOType",
                        column: x => x.IPOType,
                        principalTable: "IPO_TypeMaster",
                        principalColumn: "IPOTypeID");
                });

            migrationBuilder.CreateTable(
                name: "IPO_GroupMaster",
                columns: table => new
                {
                    IPOGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MobileNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IPOId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_GroupMaster", x => x.IPOGroupId);
                    table.ForeignKey(
                        name: "FK_IPO_GroupMaster_IPO_IPOMaster_IPOId",
                        column: x => x.IPOId,
                        principalTable: "IPO_IPOMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IPO_ClientSetup",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PANNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ClientDPId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_ClientSetup", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_IPO_ClientSetup_IPO_GroupMaster_GroupId",
                        column: x => x.GroupId,
                        principalTable: "IPO_GroupMaster",
                        principalColumn: "IPOGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IPO_PlaceOrderChild",
                columns: table => new
                {
                    POChildId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_IPO_PlaceOrderChild_IPO_GroupMaster_GroupId",
                        column: x => x.GroupId,
                        principalTable: "IPO_GroupMaster",
                        principalColumn: "IPOGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "IPO_TypeMaster",
                columns: new[] { "IPOTypeID", "IPOTypeName" },
                values: new object[,]
                {
                    { 1, "Main Board IPOs" },
                    { 2, "SME IPOs" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IPO_ApiLogs_RequestTime",
                table: "IPO_ApiLogs",
                column: "RequestTime");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_ApiLogs_StatusCode",
                table: "IPO_ApiLogs",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_BuyerOrder_BuyerMasterId",
                table: "IPO_BuyerOrder",
                column: "BuyerMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_ClientDeleteHistoryDetail_HistoryId",
                table: "IPO_ClientDeleteHistoryDetail",
                column: "HistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_ClientSetup_GroupId",
                table: "IPO_ClientSetup",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_GroupMaster_IPOId",
                table: "IPO_GroupMaster",
                column: "IPOId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_IPOMaster_IPOType",
                table: "IPO_IPOMaster",
                column: "IPOType");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_PlaceOrderChild_GroupId",
                table: "IPO_PlaceOrderChild",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_PlaceOrderChild_OrderId",
                table: "IPO_PlaceOrderChild",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_UserMasters_Email",
                table: "IPO_UserMasters",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPO_ApiLogs");

            migrationBuilder.DropTable(
                name: "IPO_ClientDeleteHistoryDetail");

            migrationBuilder.DropTable(
                name: "IPO_ClientSetup");

            migrationBuilder.DropTable(
                name: "IPO_PlaceOrderChild");

            migrationBuilder.DropTable(
                name: "IPO_UserMasters");

            migrationBuilder.DropTable(
                name: "IPO_ClientDeleteHistory");

            migrationBuilder.DropTable(
                name: "IPO_BuyerOrder");

            migrationBuilder.DropTable(
                name: "IPO_GroupMaster");

            migrationBuilder.DropTable(
                name: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.DropTable(
                name: "IPO_IPOMaster");

            migrationBuilder.DropTable(
                name: "IPO_TypeMaster");
        }
    }
}
