using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlaceOrderChild_Company_Order_Deleted",
                table: "IPO_PlaceOrderChild",
                columns: new[] { "CompanyId", "OrderId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceOrderChild_CreatedDate",
                table: "IPO_PlaceOrderChild",
                column: "ChildOrderCreatedDate",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BuyerMaster_IPO_Company_Active",
                table: "IPO_BuyerPlaceOrderMaster",
                columns: new[] { "IPOId", "CompanyId", "IsActive", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlaceOrderChild_Company_Order_Deleted",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.DropIndex(
                name: "IX_PlaceOrderChild_CreatedDate",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.DropIndex(
                name: "IX_BuyerMaster_IPO_Company_Active",
                table: "IPO_BuyerPlaceOrderMaster");
        }
    }
}
