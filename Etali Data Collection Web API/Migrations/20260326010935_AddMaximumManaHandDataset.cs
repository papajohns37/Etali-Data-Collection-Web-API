using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etali_Data_Collection_Web_API.Migrations
{
    /// <inheritdoc />
    public partial class AddMaximumManaHandDataset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maximum_mana_evaluated_hands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    Cards = table.Column<string>(type: "jsonb", nullable: false),
                    seat_order = table.Column<int>(type: "integer", nullable: false),
                    stickers = table.Column<string>(type: "jsonb", nullable: false),
                    decklist_id = table.Column<int>(type: "integer", nullable: false),
                    decklist_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    lowest_gamble_odds = table.Column<float>(type: "numeric(5,4)", nullable: false),
                    maximum_mana_in7_card_hand = table.Column<int>(type: "integer", nullable: false),
                    maximum_mana_in6_card_hand = table.Column<int>(type: "integer", nullable: false),
                    maximum_mana_in5_card_hand = table.Column<int>(type: "integer", nullable: false),
                    maximum_mana_in4_card_hand = table.Column<int>(type: "integer", nullable: false),
                    maximum_mana_in3_card_hand = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_maximum_mana_evaluated_hands", x => x.id);
                    table.CheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_3", "maximum_mana_in3_card_hand >= 0");
                    table.CheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_4", "maximum_mana_in4_card_hand >= 0");
                    table.CheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_5", "maximum_mana_in5_card_hand >= 0");
                    table.CheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_6", "maximum_mana_in6_card_hand >= 0");
                    table.CheckConstraint("ck_maximum_mana_evaluated_hands_max_mana_7", "maximum_mana_in7_card_hand >= 0");
                    table.CheckConstraint("ck_maximum_mana_evaluated_hands_seat_order", "seat_order >= 1 AND seat_order <= 4");
                    table.ForeignKey(
                        name: "FK_maximum_mana_evaluated_hands_asp_net_users_created_by_user_~",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_maximum_mana_evaluated_hands_decklists_decklist_id",
                        column: x => x.decklist_id,
                        principalTable: "decklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maximum_mana_evaluated_hands_created_by_user",
                table: "maximum_mana_evaluated_hands",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_maximum_mana_evaluated_hands_decklist_id",
                table: "maximum_mana_evaluated_hands",
                column: "decklist_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maximum_mana_evaluated_hands");
        }
    }
}
