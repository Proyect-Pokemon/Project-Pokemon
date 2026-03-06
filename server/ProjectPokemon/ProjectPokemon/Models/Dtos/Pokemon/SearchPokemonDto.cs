namespace ProjectPokemon.Models.Dtos.Pokemon {
    public class SearchPokemonDto {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string SpriteFront { get; set; }
    }
}
