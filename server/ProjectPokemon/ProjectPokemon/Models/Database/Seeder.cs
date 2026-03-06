using ProjectPokemon.Enum;
using ProjectPokemon.Helpers;
using ProjectPokemon.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProjectPokemon.Models.Database;

public class Seeder {
    private readonly PokemonDbContext _dbContext;

    public Seeder(PokemonDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        // Evitar duplicados
        if (await _dbContext.Users.AnyAsync()) return;

        // Crear usuarios
        User admin = new User { Email = "admin@example.com", Nickname = "admin", Password = PasswordHelper.Hash("admin123"), Role = "admin", AvatarPath = "/defaultAvatar.png" };
        User Ash = new User { Email = "Ash@example.com", Nickname = "Ash", Password = PasswordHelper.Hash("pikachu"), Role = "user", AvatarPath = "/defaultAvatar.png", Biography = "Sere Maestro pokemon" };
        User Misty = new User { Email = "Misty@example.com", Nickname = "Misty", Password = PasswordHelper.Hash("1111"), Role = "user", AvatarPath = "/defaultAvatar.png" };
        User Red = new User { Email = "Red@example.com", Nickname = "Red", Password = PasswordHelper.Hash("Red"), Role = "user", AvatarPath = "/defaultAvatar.png" };
        _dbContext.Users.AddRange(admin, Ash, Misty, Red);
        _dbContext.SaveChanges();

        // Crear equipos
        Team adminTeam = new Team { Name = "Admin Team", Description = "Equipo del administrador", User = admin };
        Team LegendTeam = new Team { Name = "LegendTeam", Description = "Equipo Legendario", User = admin };
        Team ashTeam = new Team { Name = "Ash's Team", Description = "Pikachu, impactrueno", User = Ash };
        Team mistyTeam = new Team { Name = "Misty's Team", Description = "Equipo de Misty", User = Misty };
        Team redTeam = new Team { Name = "Red's Team", Description = "Equipo de Red", User = Red };
        _dbContext.Teams.AddRange(adminTeam, LegendTeam, ashTeam, mistyTeam, redTeam);
        _dbContext.SaveChanges();

        // Crear Naturalezas
        Nature Hardy = new Nature { Name = PokeNature.Hardy, StatBoost = StatType.Attack, StatDrop = StatType.Attack };
        Nature Lonely = new Nature { Name = PokeNature.Lonely, StatBoost = StatType.Attack, StatDrop = StatType.Defense };
        Nature Adamant = new Nature { Name = PokeNature.Adamant, StatBoost = StatType.Attack, StatDrop = StatType.SpecialAttack };
        Nature Naughty = new Nature { Name = PokeNature.Naughty, StatBoost = StatType.Attack, StatDrop = StatType.SpecialDefense };
        Nature Brave = new Nature { Name = PokeNature.Brave, StatBoost = StatType.Attack, StatDrop = StatType.Speed };
        Nature Bold = new Nature { Name = PokeNature.Bold, StatBoost = StatType.Defense, StatDrop = StatType.Attack };
        Nature Docile = new Nature { Name = PokeNature.Docile, StatBoost = StatType.Defense, StatDrop = StatType.Defense };
        Nature Impish = new Nature { Name = PokeNature.Impish, StatBoost = StatType.Defense, StatDrop = StatType.SpecialAttack };
        Nature Lax = new Nature { Name = PokeNature.Lax, StatBoost = StatType.Defense, StatDrop = StatType.SpecialDefense };
        Nature Relaxed = new Nature { Name = PokeNature.Relaxed, StatBoost = StatType.Defense, StatDrop = StatType.Speed };
        Nature Modest = new Nature { Name = PokeNature.Modest, StatBoost = StatType.SpecialAttack, StatDrop = StatType.Attack };
        Nature Mild = new Nature { Name = PokeNature.Mild, StatBoost = StatType.SpecialAttack, StatDrop = StatType.Defense };
        Nature Bashful = new Nature { Name = PokeNature.Bashful, StatBoost = StatType.SpecialAttack, StatDrop = StatType.SpecialAttack };
        Nature Rash = new Nature { Name = PokeNature.Rash, StatBoost = StatType.SpecialAttack, StatDrop = StatType.SpecialDefense };
        Nature Quiet = new Nature { Name = PokeNature.Quiet, StatBoost = StatType.SpecialAttack, StatDrop = StatType.Speed };
        Nature Calm = new Nature { Name = PokeNature.Calm, StatBoost = StatType.SpecialDefense, StatDrop = StatType.Attack };
        Nature Gentle = new Nature { Name = PokeNature.Gentle, StatBoost = StatType.SpecialDefense, StatDrop = StatType.Defense };
        Nature Careful = new Nature { Name = PokeNature.Careful, StatBoost = StatType.SpecialDefense, StatDrop = StatType.SpecialAttack };
        Nature Quirky = new Nature { Name = PokeNature.Quirky, StatBoost = StatType.SpecialDefense, StatDrop = StatType.SpecialDefense };
        Nature Sassy = new Nature { Name = PokeNature.Sassy, StatBoost = StatType.SpecialDefense, StatDrop = StatType.Speed };
        Nature Timid = new Nature { Name = PokeNature.Timid, StatBoost = StatType.Speed, StatDrop = StatType.Attack };
        Nature Hasty = new Nature { Name = PokeNature.Hasty, StatBoost = StatType.Speed, StatDrop = StatType.Defense };
        Nature Jolly = new Nature { Name = PokeNature.Jolly, StatBoost = StatType.Speed, StatDrop = StatType.SpecialAttack };
        Nature Naive = new Nature { Name = PokeNature.Naive, StatBoost = StatType.Speed, StatDrop = StatType.SpecialDefense };
        Nature Serious = new Nature { Name = PokeNature.Serious, StatBoost = StatType.Speed, StatDrop = StatType.Speed };
        _dbContext.Natures.AddRange(Hardy, Lonely, Adamant, Naughty, Brave, Bold, Docile, Impish, Lax, Relaxed, Modest, Mild, Bashful, Rash, Quiet, Calm, Gentle, Careful, Quirky, Sassy, Timid, Hasty, Jolly, Naive, Serious);
        _dbContext.SaveChanges();

        // PokemonTeams
        PokemonTeam admin1PokemonTeam = new PokemonTeam { TeamId = adminTeam.Id, PokemonId = 150, NatureId = 1, Slot = 1, MovementId1 = 85, MovementId2 = 60, MovementId3 = 59, MovementId4 = 100, Nickname = "MewTwooSSJ100", Sex = null };
        PokemonTeam ash1PokemonTeam = new PokemonTeam { TeamId = ashTeam.Id, PokemonId = 25, NatureId = 1, Slot = 1, MovementId1 = 85, Sex = 'M' };
        PokemonTeam misty1PokemonTeam = new PokemonTeam { TeamId = mistyTeam.Id, PokemonId = 120, NatureId = 2, Slot = 1, MovementId1 = 55, Sex = 'H' };
        PokemonTeam red1PokemonTeam = new PokemonTeam { TeamId = redTeam.Id, PokemonId = 6, NatureId = 3, Slot = 1, MovementId1 = 150, Sex = 'M' };

        PokemonTeam MewAdmin = new PokemonTeam { TeamId = LegendTeam.Id, PokemonId = 151, NatureId = 1, Slot = 1, MovementId1 = 53, MovementId2 = 57, MovementId3 = 59, MovementId4 = 94, Nickname = "Mew", Sex = null };
        PokemonTeam MewTwoAdmin = new PokemonTeam { TeamId = LegendTeam.Id, PokemonId = 150, NatureId = 11, Slot = 2, MovementId1 = 105, MovementId2 = 57, MovementId3 = 96, MovementId4 = 94, Nickname = "MewTwo", Sex = null };
        PokemonTeam MoltresAdmin = new PokemonTeam { TeamId = LegendTeam.Id, PokemonId = 146, NatureId = 11, Slot = 3, MovementId1 = 83, MovementId2 = 126, MovementId3 = 97, MovementId4 = 53, Nickname = "Moltres", Sex = null };
        PokemonTeam ZapdosAdmin = new PokemonTeam { TeamId = LegendTeam.Id, PokemonId = 145, NatureId = 11, Slot = 4, MovementId1 = 86, MovementId2 = 87, MovementId3 = 85, MovementId4 = 65, Nickname = "Zapdos", Sex = null };
        PokemonTeam ArticunoAdmin = new PokemonTeam { TeamId = LegendTeam.Id, PokemonId = 144, NatureId = 1, Slot =5, MovementId1 = 59, MovementId2 = 58, MovementId3 = 62, MovementId4 = 97, Nickname = "Articuno", Sex = null };
        PokemonTeam SnorlaxAdmin = new PokemonTeam { TeamId = LegendTeam.Id, PokemonId = 143, NatureId = 3, Slot = 6, MovementId1 = 34, MovementId2 = 156, MovementId3 = 14, MovementId4 = 107, Nickname = "Snorlax", Sex = 'M' };

        _dbContext.PokemonTeams.AddRange(admin1PokemonTeam, ash1PokemonTeam, misty1PokemonTeam, red1PokemonTeam, MewAdmin, MewTwoAdmin, MoltresAdmin, ZapdosAdmin, ArticunoAdmin, SnorlaxAdmin);
        _dbContext.SaveChanges();
    }
}
