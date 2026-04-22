using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ProjectPokemon.Hubs;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Repositories;
using ProjectPokemon.Services;
using ProjectPokemon.Services.Auth;
using ProjectPokemon.Services.Internal;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using System.Text;

namespace ProjectPokemon;

public class Program
{
    public static async Task Main(string[] args)
    {
        // El directorio de trabajo será donde está el ejecutable del programa
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var builder = WebApplication.CreateBuilder(args);

        // Controladores
        builder.Services.AddControllers()
            // Esto es para que en el Swagger se muestre el texto del valor de los enum en vez de el número
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                );
            });

        // Repositorios
        builder.Services.AddScoped<UserRepository>();
        builder.Services.AddScoped<TeamRepository>();
        builder.Services.AddScoped<PokemonTeamRepository>();
        builder.Services.AddScoped<UnitOfWork>();

        // DbContext
        builder.Services.AddDbContext<PokemonDbContext>(options =>
        {
            options.UseSqlite("Data Source=pokemon.db");
        });

        // Internal Services
        builder.Services.AddScoped<DataLoader>();
        builder.Services.AddScoped<PokemonDataService>();
        builder.Services.AddScoped<MovementDataService>();

        builder.Services.AddSingleton<BattleSessionManager>();
        builder.Services.AddScoped<BattleService>();

        // SignalR
        builder.Services.AddSignalR();

        // Auth & JWT
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<AuthService>();

        builder.Services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                // Por seguridad guardamos la clave privada en variables de entorno
                // La clave debe tener más de 256 bits
                string? key = Environment.GetEnvironmentVariable("JWT_KEY");

                if (key is null)
                    throw new InvalidOperationException("La variable de entorno JWT_KEY no está definida.");

                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    // Si no nos importa que se valide el emisor del token, lo desactivamos
                    ValidateIssuer = false,
                    // Si no nos importa que se valide para quién o
                    // para qué propósito está destinado el token, lo desactivamos
                    ValidateAudience = false,
                    // Indicamos la clave
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    RoleClaimType = ClaimTypes.Role // para [Authorize(Roles="...")]
                };
            });

        // Swagger
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "Authorization",
                Description = "Introduce el token JWT",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>(
                true,
                JwtBearerDefaults.AuthenticationScheme
            );
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        }

        app.UseHttpsRedirection();              // redirige HTTP a HTTPS
        app.UseStaticFiles();                   // permite servir archivos desde wwwroot
        app.UseAuthentication();                // middleware de autenticacion
        app.UseAuthorization();                 // middleware de autorizacion
        app.MapControllers();                   // mapea los endpoints de los controladores
        app.MapHub<BattleHub>("/battlehub");    // Mapear el hub de SignalR

        // Llamar al método antes de ejecutar la app
        await SeedDatabase(app.Services);

        app.Run();
    }

    // Método del seeder y creación de la base de datos
    private static async Task SeedDatabase(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();
        var dataLoader = scope.ServiceProvider.GetRequiredService<DataLoader>();

        if (dbContext.Database.EnsureCreated())  // Esto crea la DB si no existe
        {
            await dataLoader.LoadAllDataAsync();

            Seeder seeder = new Seeder(dbContext);
            await seeder.SeedAsync();
        }
    }
}