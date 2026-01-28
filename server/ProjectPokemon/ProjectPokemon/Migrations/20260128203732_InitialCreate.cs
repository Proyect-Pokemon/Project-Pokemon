using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPokemon.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Pp = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementClass = table.Column<string>(type: "TEXT", nullable: false),
                    Accuracy = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    Power = table.Column<int>(type: "INTEGER", nullable: false),
                    Contact = table.Column<bool>(type: "INTEGER", nullable: false),
                    Target = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Natures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StatBoost = table.Column<string>(type: "TEXT", nullable: false),
                    StatDrop = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Natures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pokemons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Hp = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialAttack = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialDefense = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    SpriteFront = table.Column<string>(type: "TEXT", nullable: false),
                    SpriteBack = table.Column<string>(type: "TEXT", nullable: false),
                    SpriteFrontShiny = table.Column<string>(type: "TEXT", nullable: true),
                    SpriteBackShiny = table.Column<string>(type: "TEXT", nullable: true),
                    SpriteFrontFem = table.Column<string>(type: "TEXT", nullable: true),
                    SpriteBackFem = table.Column<string>(type: "TEXT", nullable: true),
                    SpriteFrontFemShiny = table.Column<string>(type: "TEXT", nullable: true),
                    SpriteBackFemShiny = table.Column<string>(type: "TEXT", nullable: true),
                    Cry = table.Column<string>(type: "TEXT", nullable: true),
                    Type1 = table.Column<string>(type: "TEXT", nullable: false),
                    Type2 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pokemons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PokemonBattles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Team = table.Column<int>(type: "INTEGER", nullable: false),
                    Shiny = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PokemonId = table.Column<int>(type: "INTEGER", nullable: false),
                    NatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId1 = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId2 = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId3 = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "None")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonBattles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonBattles_Movements_MovementId1",
                        column: x => x.MovementId1,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PokemonBattles_Movements_MovementId2",
                        column: x => x.MovementId2,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PokemonBattles_Movements_MovementId3",
                        column: x => x.MovementId3,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PokemonBattles_Movements_MovementId4",
                        column: x => x.MovementId4,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PokemonBattles_Natures_NatureId",
                        column: x => x.NatureId,
                        principalTable: "Natures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PokemonBattles_Pokemons_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PokemonMovements",
                columns: table => new
                {
                    PokemonId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonMovements", x => new { x.PokemonId, x.MovementId });
                    table.ForeignKey(
                        name: "FK_PokemonMovements_Movements_MovementId",
                        column: x => x.MovementId,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonMovements_Pokemons_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokemonBattles_MovementId1",
                table: "PokemonBattles",
                column: "MovementId1");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonBattles_MovementId2",
                table: "PokemonBattles",
                column: "MovementId2");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonBattles_MovementId3",
                table: "PokemonBattles",
                column: "MovementId3");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonBattles_MovementId4",
                table: "PokemonBattles",
                column: "MovementId4");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonBattles_NatureId",
                table: "PokemonBattles",
                column: "NatureId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonBattles_PokemonId",
                table: "PokemonBattles",
                column: "PokemonId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonMovements_MovementId",
                table: "PokemonMovements",
                column: "MovementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokemonBattles");

            migrationBuilder.DropTable(
                name: "PokemonMovements");

            migrationBuilder.DropTable(
                name: "Natures");

            migrationBuilder.DropTable(
                name: "Movements");

            migrationBuilder.DropTable(
                name: "Pokemons");
        }
    }
}
