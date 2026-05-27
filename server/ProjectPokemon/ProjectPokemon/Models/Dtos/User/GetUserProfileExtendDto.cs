namespace SocialNetwork.Models.Dtos.Users {
    public class GetUserProfileExtendDto {
        public required string Email { get; set; }
        public required string Nickname { get; set; }
        public string? Biography { get; set; }
        public int? FavoriteTeamId { get; set; }
    }
}
