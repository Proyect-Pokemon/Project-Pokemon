namespace ProjectPokemon.Models.Dtos.Admin;

public class GetAdminUserDto {
    public int Id { get; set; }
    public required string Nickname { get; set; }
    public required string Email { get; set; }
    public string Role { get; set; } = "user";
    public string? AvatarPath { get; set; }
    public DateTime CreationDate { get; set; }
}