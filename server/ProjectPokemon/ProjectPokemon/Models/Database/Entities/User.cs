using Microsoft.EntityFrameworkCore;

namespace ProjectPokemon.Models.Database.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Nickname), IsUnique = true)]
public class User {
    public long Id { get; set; }
    public required string Email { get; set; }
    public required string Nickname { get; set; }
    public required string Password { get; set; }
    public string Role { get; set; } = "user";
    public string? AvatarPath { get; set; } = null;
    public string? Biography { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    //public int VictoryCount { get; set; } = 0;
    //TO DO: Shiny token

    public ICollection<Team> Teams { get; set; } = [];
}