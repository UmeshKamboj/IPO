using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "IPO_GroupMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "IPO_GroupMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileNo",
                table: "IPO_GroupMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "IPO_GroupMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ApplicateRate",
                table: "IPO_BuyerOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster",
                column: "GroupId",
                principalTable: "IPO_GroupMaster",
                principalColumn: "IPOGroupId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IPO_BuyerPlaceOrderMaster_IPO_GroupMaster_GroupId",
                table: "IPO_BuyerPlaceOrderMaster");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "IPO_GroupMaster");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "IPO_GroupMaster");

            migrationBuilder.DropColumn(
                name: "MobileNo",
                table: "IPO_GroupMaster");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "IPO_GroupMaster");

            migrationBuilder.DropColumn(
                name: "ApplicateRate",
                table: "IPO_BuyerOrder");

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
