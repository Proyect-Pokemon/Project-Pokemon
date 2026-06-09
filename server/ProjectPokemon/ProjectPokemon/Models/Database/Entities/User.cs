using Microsoft.EntityFrameworkCore;

namespace ProjectPokemon.Models.Database.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Nickname), IsUnique = true)]
public class User {
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Nickname { get; set; }
    public required string Password { get; set; }
    public string Role { get; set; } = "user";
    public string? AvatarPath { get; set; } = null;
    public string? Biography { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public string? GoogleId { get; set; }
    public int? FavoriteTeamId { get; set; }
    //public int VictoryCount { get; set; } = 0;
    public Team? FavoriteTeam { get; set; }
    public ICollection<Team> Teams { get; set; } = [];
}