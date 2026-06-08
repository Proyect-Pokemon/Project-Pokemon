namespace ProjectPokemon.Models.Dtos.User {
    public class GetUserProfileExtendDto {
        public required string Email { get; set; }
        public required string Nickname { get; set; }
        public string? Biography { get; set; }
        public string? AvatarPath { get; set; } = null;
        public int? FavoriteTeamId { get; set; }
    }
}