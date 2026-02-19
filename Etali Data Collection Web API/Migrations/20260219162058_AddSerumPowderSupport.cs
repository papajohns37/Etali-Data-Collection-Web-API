using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Etali_Data_Collection_Web_API.Migrations
{
    /// <inheritdoc />
    public partial class AddSerumPowderSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "total_serum_powder_activations",
                table: "full_mulligan_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "serum_powder_activations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    hand_number = table.Column<int>(type: "integer", nullable: false),
                    target_hand_size = table.Column<int>(type: "integer", nullable: false),
                    CardsExiled = table.Column<string>(type: "jsonb", nullable: false),
                    CardsBottomed = table.Column<string>(type: "jsonb", nullable: false),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_serum_powder_activations", x => x.id);
                    table.CheckConstraint("ck_serum_powder_activations_hand_number", "hand_number >= 1 AND hand_number <= 6");
                    table.CheckConstraint("ck_serum_powder_activations_target_hand_size", "target_hand_size IN (3, 4, 5, 6, 7)");
                    table.ForeignKey(
                        name: "FK_serum_powder_activations_full_mulligan_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "full_mulligan_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_serum_powder_activations_session_hand",
                table: "serum_powder_activations",
                columns: new[] { "session_id", "hand_number" });

            migrationBuilder.CreateIndex(
                name: "ix_serum_powder_activations_session_id",
                table: "serum_powder_activations",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "serum_powder_activations");

            migrationBuilder.DropColumn(
                name: "total_serum_powder_activations",
                table: "full_mulligan_sessions");
        }
    }
}
