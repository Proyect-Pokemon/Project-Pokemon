namespace ProjectPokemon.Models.Dtos.User {
    public class PutPasswordDto {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}