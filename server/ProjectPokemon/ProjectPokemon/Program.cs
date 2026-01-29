using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Services.Internal;

namespace ProjectPokemon;

public class Program
{
    public async static Task Main(string[] args) {
        // El directorio de trabajo será donde está el ejecutable del programa
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var builder = WebApplication.CreateBuilder(args);

        // DbContext
        builder.Services.AddDbContext<PokemonDbContext>(options => {
            options.UseSqlite("Data Source=pokemon.db");
        });

        // Services
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped<DataLoader>();
        builder.Services.AddScoped<PokemonDataService>();

        var app = builder.Build();

        // Inicializar la base de datos
        using (IServiceScope scope = app.Services.CreateScope()) {
            PokemonDbContext dbContext = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();
            DataLoader dataLoader = scope.ServiceProvider.GetRequiredService<DataLoader>();
            if (dbContext.Database.EnsureCreated()) {
                await dataLoader.LoadAllDataAsync();
            }
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.MapOpenApi();
            app.UseCors(policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}