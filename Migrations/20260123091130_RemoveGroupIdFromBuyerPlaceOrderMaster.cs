using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGroupIdFromBuyerPlaceOrderMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.DropIndex(
                name: "IX_IPO_BuyerPlaceOrderMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_PlaceOrderChild_GroupId",
                table: "IPO_PlaceOrderChild",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_IPO_PlaceOrderChild_IPO_GroupMaster_GroupId",
                table: "IPO_PlaceOrderChild",
                column: "GroupId",
                principalTable: "IPO_GroupMaster",
                principalColumn: "IPOGroupId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IPO_PlaceOrderChild_IPO_GroupMaster_GroupId",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.DropIndex(
                name: "IX_IPO_PlaceOrderChild_GroupId",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_IPO_BuyerPlaceOrderMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                column: "GroupId",
                principalTable: "IPO_GroupMaster",
                principalColumn: "IPOGroupId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
