using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Etali_Data_Collection_Web_API.Migrations
{
    /// <inheritdoc />
    public partial class AddFullMulliganTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "overall_deck1_n",
                table: "deck_comparisons");

            migrationBuilder.DropColumn(
                name: "overall_deck2_n",
                table: "deck_comparisons");

            migrationBuilder.CreateTable(
                name: "full_mulligan_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decklist_id = table.Column<int>(type: "integer", nullable: false),
                    decklist_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    seat_order = table.Column<int>(type: "integer", nullable: false),
                    stickers = table.Column<string>(type: "jsonb", nullable: false),
                    lowest_gamble_odds = table.Column<float>(type: "numeric(5,4)", nullable: false),
                    found_successful_hand = table.Column<bool>(type: "boolean", nullable: false),
                    final_hand_size = table.Column<int>(type: "integer", nullable: false),
                    total_mulligans = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_full_mulligan_sessions", x => x.id);
                    table.CheckConstraint("ck_full_mulligan_sessions_final_hand_size", "final_hand_size >= 0 AND final_hand_size <= 7");
                    table.CheckConstraint("ck_full_mulligan_sessions_seat_order", "seat_order >= 1 AND seat_order <= 4");
                    table.CheckConstraint("ck_full_mulligan_sessions_total_mulligans", "total_mulligans >= 0 AND total_mulligans <= 5");
                    table.ForeignKey(
                        name: "FK_full_mulligan_sessions_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_full_mulligan_sessions_decklists_decklist_id",
                        column: x => x.decklist_id,
                        principalTable: "decklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mulligan_hand_evaluations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    hand_number = table.Column<int>(type: "integer", nullable: false),
                    target_hand_size = table.Column<int>(type: "integer", nullable: false),
                    Cards = table.Column<string>(type: "jsonb", nullable: false),
                    can_cast_etali_by_turn2 = table.Column<bool>(type: "boolean", nullable: false),
                    kept_hand = table.Column<bool>(type: "boolean", nullable: false),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_mulligan_hand_evaluations", x => x.id);
                    table.CheckConstraint("ck_mulligan_hand_evaluations_hand_number", "hand_number >= 1 AND hand_number <= 6");
                    table.CheckConstraint("ck_mulligan_hand_evaluations_target_hand_size", "target_hand_size IN (3, 4, 5, 6, 7)");
                    table.ForeignKey(
                        name: "FK_mulligan_hand_evaluations_full_mulligan_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "full_mulligan_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_full_mulligan_sessions_created_at",
                table: "full_mulligan_sessions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_full_mulligan_sessions_created_by_user",
                table: "full_mulligan_sessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_full_mulligan_sessions_decklist_active",
                table: "full_mulligan_sessions",
                columns: new[] { "decklist_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_full_mulligan_sessions_decklist_id",
                table: "full_mulligan_sessions",
                column: "decklist_id");

            migrationBuilder.CreateIndex(
                name: "ix_mulligan_hand_evaluations_session_hand_unique",
                table: "mulligan_hand_evaluations",
                columns: new[] { "session_id", "hand_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mulligan_hand_evaluations_session_id",
                table: "mulligan_hand_evaluations",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mulligan_hand_evaluations");

            migrationBuilder.DropTable(
                name: "full_mulligan_sessions");

            migrationBuilder.AddColumn<int>(
                name: "overall_deck1_n",
                table: "deck_comparisons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "overall_deck2_n",
                table: "deck_comparisons",
                type: "integer",
                nullable: true);
        }
    }
}
