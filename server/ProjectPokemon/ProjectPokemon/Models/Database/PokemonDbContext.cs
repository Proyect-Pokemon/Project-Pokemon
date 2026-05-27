using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database;
public class PokemonDbContext : DbContext {
    public DbSet<Pokemon> Pokemons { get; set; }
    public DbSet<Movement> Movements { get; set; }
    public DbSet<Nature> Natures { get; set; }
    public DbSet<PokemonMovement> PokemonMovements { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Team> Teams { get; set; } 
    public DbSet<PokemonTeam> PokemonTeams { get; set; }

    public PokemonDbContext(DbContextOptions<PokemonDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ConfigurePokemon(modelBuilder);
        ConfigureMovement(modelBuilder);
        ConfigureNature(modelBuilder);
        ConfigurePokemonMovement(modelBuilder);
        ConfigureTeam(modelBuilder);
    }

    private static void ConfigureTeam(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(entity => {
            entity.HasOne(u => u.FavoriteTeam)
                  .WithMany()
                  .HasForeignKey(u => u.FavoriteTeamId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePokemon(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Pokemon>(entity =>
        {
            //Para guardar los enum como string, no como int
            entity.Property(p => p.Type1)
                    .HasConversion<string>()
                    .IsRequired();

            entity.Property(p => p.Type2)
                    .HasConversion<string>();
        });
    }
    private static void ConfigureMovement(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Movement>(entity =>
        {
            entity.Property(m => m.MovementClass)
                    .HasConversion<string>()
                    .IsRequired();

            entity.Property(m => m.Target)
                    .HasConversion<string>()
                    .IsRequired();

            entity.Property(m => m.Type)
                    .HasConversion<string>()
                    .IsRequired();
        });
    }
    private static void ConfigureNature(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Nature>(entity =>
        {
            entity.Property(n => n.Name)
                    .HasConversion<string>()
                    .IsRequired();

            entity.Property(n => n.StatBoost)
                    .HasConversion<string>()
                    .IsRequired();

            entity.Property(n => n.StatDrop)
                    .HasConversion<string>()
                    .IsRequired();
        });
    }
    private static void ConfigurePokemonMovement(ModelBuilder modelBuilder) {
        modelBuilder.Entity<PokemonMovement>(entity => {
            entity.HasKey(pm => new { pm.PokemonId, pm.MovementId });
            // Relación con Pokemon
            entity.HasOne(pm => pm.Pokemon)
                    .WithMany(p => p.PokemonMovements)
                    .HasForeignKey(pm => pm.PokemonId)
                    .OnDelete(DeleteBehavior.Cascade);
                    // Si el Pokemon se elimina, se elimina la fila

            // Relación con Movement
            entity.HasOne(pm => pm.Movement)
                    .WithMany(m => m.PokemonMovements)
                    .HasForeignKey(pm => pm.MovementId)
                    .OnDelete(DeleteBehavior.Cascade);
                    // Si el Movimiento se elimina, se elimina la fila
        });
    }
}