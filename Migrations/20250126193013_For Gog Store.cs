using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace price_comparator_site.Migrations
{
    /// <inheritdoc />
    public partial class ForGogStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreUrl",
                table: "Games",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreUrl",
                table: "Games");
        }
    }
}
