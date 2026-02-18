using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPOClient.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrarCacheTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPO_RegistrarCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrarName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CachedIposJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CachedIpoCount = table.Column<int>(type: "int", nullable: false),
                    LastFetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastFailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPO_RegistrarCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IPO_RegistrarCache_RegistrarName",
                table: "IPO_RegistrarCache",
                column: "RegistrarName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPO_RegistrarCache");
        }
    }
}
