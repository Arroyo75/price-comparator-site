using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace price_comparator_site.Migrations
{
    /// <inheritdoc />
    public partial class GogId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GogId",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GogId",
                table: "Games");
        }
    }
}
