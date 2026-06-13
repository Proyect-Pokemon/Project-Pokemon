namespace ProjectPokemon.Models.Dtos.User {
    public class GetUserDto {
        public int Id { get; set; }
        public required string Nickname { get; set; }
        public string AvatarPath { get; set; } = "/defaultAvatar.png";
    }
}