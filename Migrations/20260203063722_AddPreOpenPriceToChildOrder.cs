using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddPreOpenPriceToChildOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PreOpenPrice",
                table: "IPO_PlaceOrderChild",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreOpenPrice",
                table: "IPO_PlaceOrderChild");
        }
    }
}
