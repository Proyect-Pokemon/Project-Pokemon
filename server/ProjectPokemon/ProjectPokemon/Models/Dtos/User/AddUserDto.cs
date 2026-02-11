namespace ProjectPokemon.Models.Dtos.User {
    public class AddUserDto {
        public required string Email { get; set; }
        public required string Nickname { get; set; }
        public required string Password { get; set; }
        public string? AvatarPath { get; set; } = null;
        public string? Biography { get; set; }
    }
}
