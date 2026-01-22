using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenIPOPriceToIPOMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OpenIPOPrice",
                table: "IPO_IPOMaster",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            // Update existing records to set OpenIPOPrice = IPO_Upper_Price_Band
            migrationBuilder.Sql(
                "UPDATE IPO_IPOMaster SET OpenIPOPrice = IPO_Upper_Price_Band WHERE OpenIPOPrice = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenIPOPrice",
                table: "IPO_IPOMaster");
        }
    }
}
