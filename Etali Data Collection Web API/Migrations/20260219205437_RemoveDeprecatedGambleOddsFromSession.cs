using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etali_Data_Collection_Web_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeprecatedGambleOddsFromSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lowest_gamble_odds",
                table: "full_mulligan_sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "lowest_gamble_odds",
                table: "full_mulligan_sessions",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
