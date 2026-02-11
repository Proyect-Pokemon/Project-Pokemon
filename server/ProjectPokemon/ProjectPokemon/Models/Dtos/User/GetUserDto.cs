namespace ProjectPokemon.Models.Dtos.User {
    public class GetUserDto {
        public long Id { get; set; }
        public required string Nickname { get; set; }
        public required string Password { get; set; }
    }
}
