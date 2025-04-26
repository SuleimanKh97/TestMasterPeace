using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestMasterPeace.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSoldToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSold",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSold",
                table: "Products");
        }
    }
}
