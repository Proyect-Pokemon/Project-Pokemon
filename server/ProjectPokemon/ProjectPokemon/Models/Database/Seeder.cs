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

        _dbContext.Users.AddRange(admin, Ash, Misty, Red);
        _dbContext.SaveChanges();
    }
}
