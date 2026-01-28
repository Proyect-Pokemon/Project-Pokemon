using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database; 
public class PokemonDbContext : DbContext {
    public DbSet<Pokemon> Pokemons => Set<Pokemon>();
    public DbSet<Movement> Movements => Set<Movement>();
    public DbSet<Nature> Natures => Set<Nature>();
    public DbSet<PokemonMovement> PokemonMovements => Set<PokemonMovement>();
    public DbSet<PokemonBattle> PokemonBattles => Set<PokemonBattle>();

    public PokemonDbContext(DbContextOptions<PokemonDbContext> options) : base(options) {

    }
    protected override void onModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ConfigurePokemon(modelBuilder);
        ConfigureMovement(modelBuilder);
        ConfigureNature(modelBuilder);
        ConfigurePokemonMovement(modelBuilder);
        ConfigurePokemonBattle(modelBuilder);
    }

    private static void ConfigurePokemon(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Pokemon>(entity =>
        {
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Hp).IsRequired();
            entity.Property(p => p.Attack).IsRequired();
            entity.Property(p => p.Defense).IsRequired();
            entity.Property(p => p.SpecialAttack).IsRequired();
            entity.Property(p => p.SpecialDefense).IsRequired();
            entity.Property(p => p.Speed).IsRequired();
            entity.Property(p => p.Weight).IsRequired();
            entity.Property(p => p.SpriteFront).IsRequired();
            entity.Property(p => p.SpriteBack).IsRequired();
            entity.Property(p => p.SpriteFrontShiny).IsRequired();
            entity.Property(p => p.SpriteBackShiny).IsRequired();
            entity.Property(p => p.Cry).IsRequired();

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
            entity.Property(m => m.Name).IsRequired();
            entity.Property(m => m.Description).IsRequired();
            entity.Property(m => m.Pp);
            entity.Property(m => m.Accuracy).HasDefaultValue(100);
            entity.Property(m => m.Power).HasDefaultValue(0);
            entity.Property(m => m.Priority).HasDefaultValue(0);
            entity.Property(m => m.Contact);

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
                    .WithMany(p => p.PokemonMovevements)
                    .HasForeignKey(pm => pm.PokemonId)
                    .OnDelete(DeleteBehavior.Cascade);
                    // Si el Pokemon se elimina, se elimina la fila

            // Relación con Movement
            entity.HasOne(pm => pm.Movement)
                    .WithMany(m => m.PokemonMovements)
                    .HasForeignKey(pm => pm.PokemonId)
                    .OnDelete(DeleteBehavior.Cascade);
                    // Si el Movimiento se elimina, se elimina la fila
        });
    }
    private static void ConfigurePokemonBattle(ModelBuilder modelBuilder) {
        modelBuilder.Entity<PokemonBattle>(entity =>
        {
            entity.Property(pb => pb.Shiny).HasDefaultValue(false);
            entity.Property(pb => pb.Status)
                    .HasConversion<string>()
                    .HasDefaultValue(PokeStatus.None)
                    .IsRequired();
            
            entity.HasOne(pb => pb.Nature)
                    .WithMany(n => n.PokemonBattles)
                    .HasForeignKey(pb => pb.NatureId)
                    .OnDelete(DeleteBehavior.Restrict); // No se podrá eliminar nature si tiene pokemon relacionados
            
            entity.HasOne(pb => pb.Movement1)
                    .WithMany()
                    .HasForeignKey(pb => pb.MovementId1)
                    .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(pb => pb.Movement2)
                    .WithMany()
                    .HasForeignKey(pb => pb.MovementId2)
                    .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(pb => pb.Movement3)
                    .WithMany()
                    .HasForeignKey(pb => pb.MovementId3)
                    .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(pb => pb.Movement4)
                    .WithMany()
                    .HasForeignKey(pb => pb.MovementId4)
                    .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(pb => pb.Pokemon)
                    .WithMany(p => p.PokemonBattles)
                    .HasForeignKey(pb => pb.PokemonId)
                    .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
