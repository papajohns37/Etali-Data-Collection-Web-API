using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Etali_Data_Collection_Web_API.Migrations
{
    /// <inheritdoc />
    public partial class AddOldEvaluatedHandsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "old_evaluated_hands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decklist_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    decklist_id = table.Column<int>(type: "integer", nullable: true),
                    user = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    can_cast_etali = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_cards_to_cast_etali = table.Column<int>(type: "integer", nullable: false),
                    card1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card3 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card4 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card5 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card6 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card7 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_old_evaluated_hands", x => x.id);
                    table.CheckConstraint("ck_old_evaluated_hands_min_cards", "minimum_cards_to_cast_etali >= 1 AND minimum_cards_to_cast_etali <= 8");
                    table.ForeignKey(
                        name: "FK_old_evaluated_hands_decklists_decklist_id",
                        column: x => x.decklist_id,
                        principalTable: "decklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_old_evaluated_hands_decklist_id",
                table: "old_evaluated_hands",
                column: "decklist_id");

            migrationBuilder.CreateIndex(
                name: "ix_old_evaluated_hands_decklist_name",
                table: "old_evaluated_hands",
                column: "decklist_name");

            migrationBuilder.CreateIndex(
                name: "ix_old_evaluated_hands_user",
                table: "old_evaluated_hands",
                column: "user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "old_evaluated_hands");
        }
    }
}
