using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Etali_Data_Collection_Web_API.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQLMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asp_net_roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "f_k_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "f_k_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "f_k_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_roles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "f_k_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_tokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "f_k_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "decklists",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Cards = table.Column<string>(type: "jsonb", nullable: false),
                    moxfield_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    moxfield_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_decklists", x => x.id);
                    table.ForeignKey(
                        name: "FK_decklists_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "deck_comparisons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    deck1_id = table.Column<int>(type: "integer", nullable: false),
                    deck1_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    deck2_id = table.Column<int>(type: "integer", nullable: false),
                    deck2_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    deck1_n = table.Column<int>(type: "integer", nullable: false),
                    deck2_n = table.Column<int>(type: "integer", nullable: false),
                    overall_deck1_n = table.Column<int>(type: "integer", nullable: true),
                    overall_deck2_n = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_deck_comparisons", x => x.id);
                    table.CheckConstraint("ck_deck_comparisons_deck_order", "deck1_id < deck2_id");
                    table.ForeignKey(
                        name: "fk_deck_comparisons_decklists_deck1_id",
                        column: x => x.deck1_id,
                        principalTable: "decklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deck_comparisons_decklists_deck2_id",
                        column: x => x.deck2_id,
                        principalTable: "decklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evaluated_hands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    Cards = table.Column<string>(type: "jsonb", nullable: false),
                    seat_order = table.Column<int>(type: "integer", nullable: false),
                    stickers = table.Column<string>(type: "jsonb", nullable: false),
                    can_cast_etali = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_cards_to_cast_etali = table.Column<int>(type: "integer", nullable: false),
                    decklist_id = table.Column<int>(type: "integer", nullable: false),
                    decklist_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    lowest_gamble_odds = table.Column<float>(type: "numeric(5,4)", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_evaluated_hands", x => x.id);
                    table.CheckConstraint("ck_evaluated_hands_min_cards", "minimum_cards_to_cast_etali >= 1 AND minimum_cards_to_cast_etali <= 8");
                    table.CheckConstraint("ck_evaluated_hands_seat_order", "seat_order >= 1 AND seat_order <= 4");
                    table.ForeignKey(
                        name: "FK_evaluated_hands_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_evaluated_hands_decklists_decklist_id",
                        column: x => x.decklist_id,
                        principalTable: "decklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comparison_evaluated_hands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    comparison_id = table.Column<int>(type: "integer", nullable: false),
                    which_deck = table.Column<int>(type: "integer", nullable: false),
                    Cards = table.Column<string>(type: "jsonb", nullable: false),
                    seat_order = table.Column<int>(type: "integer", nullable: false),
                    stickers = table.Column<string>(type: "jsonb", nullable: false),
                    can_cast_etali = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_cards_to_cast_etali = table.Column<int>(type: "integer", nullable: false),
                    decklist_id = table.Column<int>(type: "integer", nullable: false),
                    decklist_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    lowest_gamble_odds = table.Column<float>(type: "numeric(5,4)", nullable: false),
                    generation_attempts = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_comparison_evaluated_hands", x => x.id);
                    table.CheckConstraint("ck_comparison_evaluated_hands_gen_attempts", "generation_attempts >= 1");
                    table.CheckConstraint("ck_comparison_evaluated_hands_min_cards", "minimum_cards_to_cast_etali >= 1 AND minimum_cards_to_cast_etali <= 8");
                    table.CheckConstraint("ck_comparison_evaluated_hands_seat_order", "seat_order >= 1 AND seat_order <= 4");
                    table.CheckConstraint("ck_comparison_evaluated_hands_which_deck", "which_deck >= 1 AND which_deck <= 2");
                    table.ForeignKey(
                        name: "FK_comparison_evaluated_hands_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_comparison_evaluated_hands_deck_comparisons_comparison_id",
                        column: x => x.comparison_id,
                        principalTable: "deck_comparisons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_asp_net_role_claims_role_id",
                table: "asp_net_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "role_name_index",
                table: "asp_net_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_asp_net_user_claims_user_id",
                table: "asp_net_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_asp_net_user_logins_user_id",
                table: "asp_net_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_asp_net_user_roles_role_id",
                table: "asp_net_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "email_index",
                table: "asp_net_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "user_name_index",
                table: "asp_net_users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comparison_evaluated_hands_comparison_deck",
                table: "comparison_evaluated_hands",
                columns: new[] { "comparison_id", "which_deck" });

            migrationBuilder.CreateIndex(
                name: "ix_comparison_evaluated_hands_comparison_id",
                table: "comparison_evaluated_hands",
                column: "comparison_id");

            migrationBuilder.CreateIndex(
                name: "ix_comparison_evaluated_hands_created_by_user",
                table: "comparison_evaluated_hands",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deck_comparisons_created_at",
                table: "deck_comparisons",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_deck_comparisons_deck_ids_unique",
                table: "deck_comparisons",
                columns: new[] { "deck1_id", "deck2_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deck_comparisons_deck1_id",
                table: "deck_comparisons",
                column: "deck1_id");

            migrationBuilder.CreateIndex(
                name: "ix_deck_comparisons_deck2_id",
                table: "deck_comparisons",
                column: "deck2_id");

            migrationBuilder.CreateIndex(
                name: "ix_decklists_created_at",
                table: "decklists",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_decklists_created_by_user",
                table: "decklists",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_decklists_moxfield_id",
                table: "decklists",
                column: "moxfield_id");

            migrationBuilder.CreateIndex(
                name: "ix_decklists_name",
                table: "decklists",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_evaluated_hands_created_by_user",
                table: "evaluated_hands",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluated_hands_decklist_can_cast",
                table: "evaluated_hands",
                columns: new[] { "decklist_id", "can_cast_etali" });

            migrationBuilder.CreateIndex(
                name: "ix_evaluated_hands_decklist_id",
                table: "evaluated_hands",
                column: "decklist_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asp_net_role_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_logins");

            migrationBuilder.DropTable(
                name: "asp_net_user_roles");

            migrationBuilder.DropTable(
                name: "asp_net_user_tokens");

            migrationBuilder.DropTable(
                name: "comparison_evaluated_hands");

            migrationBuilder.DropTable(
                name: "evaluated_hands");

            migrationBuilder.DropTable(
                name: "asp_net_roles");

            migrationBuilder.DropTable(
                name: "deck_comparisons");

            migrationBuilder.DropTable(
                name: "decklists");

            migrationBuilder.DropTable(
                name: "asp_net_users");
        }
    }
}
