using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class MakeIpoIdNullableInPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_IPO_IPOMaster_IpoId",
                table: "PaymentTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "IpoId",
                table: "PaymentTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_IPO_IPOMaster_IpoId",
                table: "PaymentTransactions",
                column: "IpoId",
                principalTable: "IPO_IPOMaster",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_IPO_IPOMaster_IpoId",
                table: "PaymentTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "IpoId",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_IPO_IPOMaster_IpoId",
                table: "PaymentTransactions",
                column: "IpoId",
                principalTable: "IPO_IPOMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
