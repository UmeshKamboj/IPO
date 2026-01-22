using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupForeignKeyToBuyerMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChildPlaceOrder_IPO_BuyerOrder_IPOOrderOrderId",
                table: "ChildPlaceOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildPlaceOrder",
                table: "ChildPlaceOrder");

            migrationBuilder.DropIndex(
                name: "IX_ChildPlaceOrder_IPOOrderOrderId",
                table: "ChildPlaceOrder");

            migrationBuilder.DropColumn(
                name: "IPOOrderOrderId",
                table: "ChildPlaceOrder");

            migrationBuilder.RenameTable(
                name: "ChildPlaceOrder",
                newName: "IPO_PlaceOrderChild");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IPO_PlaceOrderChild",
                table: "IPO_PlaceOrderChild",
                column: "POChildId");

            migrationBuilder.CreateIndex(
                name: "IX_IPO_PlaceOrderChild_OrderId",
                table: "IPO_PlaceOrderChild",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                column: "GroupId",
                principalTable: "IPO_GroupMaster",
                principalColumn: "IPOGroupId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IPO_PlaceOrderChild_IPO_BuyerOrder_OrderId",
                table: "IPO_PlaceOrderChild",
                column: "OrderId",
                principalTable: "IPO_BuyerOrder",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_IPO_PlaceOrderChild_IPO_BuyerOrder_OrderId",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IPO_PlaceOrderChild",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.DropIndex(
                name: "IX_IPO_PlaceOrderChild_OrderId",
                table: "IPO_PlaceOrderChild");

            migrationBuilder.RenameTable(
                name: "IPO_PlaceOrderChild",
                newName: "ChildPlaceOrder");

            migrationBuilder.AddColumn<int>(
                name: "IPOOrderOrderId",
                table: "ChildPlaceOrder",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildPlaceOrder",
                table: "ChildPlaceOrder",
                column: "POChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPlaceOrder_IPOOrderOrderId",
                table: "ChildPlaceOrder",
                column: "IPOOrderOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChildPlaceOrder_IPO_BuyerOrder_IPOOrderOrderId",
                table: "ChildPlaceOrder",
                column: "IPOOrderOrderId",
                principalTable: "IPO_BuyerOrder",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                column: "GroupId",
                principalTable: "IPO_GroupMaster",
                principalColumn: "IPOGroupId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
