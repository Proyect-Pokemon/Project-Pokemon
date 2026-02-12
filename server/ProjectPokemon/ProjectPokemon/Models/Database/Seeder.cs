using ProjectPokemon.Helpers;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database;

public class Seeder {
    private readonly PokemonDbContext _dbContext;

    public Seeder(PokemonDbContext dbContext) {
        _dbContext = dbContext;
    }

    public void Seed() {
        // Crear usuarios
        User admin = new User { Email = "admin@example.com", Nickname = "admin", Password = PasswordHelper.Hash("admin123"), Role = "admin", AvatarPath = "/defaultAvatar.png" };
        User Ash = new User { Email = "Ash@example.com", Nickname = "Ash", Password = PasswordHelper.Hash("pikachu"), Role = "user", AvatarPath = "/defaultAvatar.png", Biography = "Sere Maestro pokemon" };
        User Misty = new User { Email = "Misty@example.com", Nickname = "Misty", Password = PasswordHelper.Hash("1111"), Role = "user", AvatarPath = "/defaultAvatar.png" };
        User Red = new User { Email = "Red@example.com", Nickname = "Red", Password = PasswordHelper.Hash("Red"), Role = "user", AvatarPath = "/defaultAvatar.png" };

        // Crear equipos
        Team adminTeam = new Team { Name = "Admin Team", Description = "Equipo del administrador", User = admin }; 
        Team ashTeam = new Team { Name = "Ash's Team", Description = "Pikachu, impactrueno", User = Ash }; 
        Team mistyTeam = new Team { Name = "Misty's Team", Description = "Equipo de Misty", User = Misty }; 
        Team redTeam = new Team { Name = "Red's Team", Description = "Equipo de Red", User = Red };

        // PokemonTeams
        PokemonTeam admin1PokemonTeam = new PokemonTeam { Team = adminTeam, PokemonId = 150, NatureId = 1, Slot = 1, MovementId1 = 85};
        PokemonTeam ash1PokemonTeam = new PokemonTeam { Team = ashTeam, PokemonId = 25, NatureId = 1, Slot = 1, MovementId1 = 85}; 
        PokemonTeam misty1PokemonTeam = new PokemonTeam { Team = mistyTeam, PokemonId = 120, NatureId = 2, Slot = 1, MovementId1 = 55}; 
        PokemonTeam red1PokemonTeam = new PokemonTeam { Team = redTeam, PokemonId = 6, NatureId = 3, Slot = 1, MovementId1 = 150};
        
        
        _dbContext.SaveChanges();
    }
}
