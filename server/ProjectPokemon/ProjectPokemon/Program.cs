using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Repositories;
using ProjectPokemon.Networking;
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

        // Auth & JWT
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<AuthService>();

        // Battle Services - WebSocket Nativo
        builder.Services.AddSingleton<ProjectPokemon.Services.BattleSessionManager>();
        builder.Services.AddScoped<ProjectPokemon.Services.BattleService>();
        builder.Services.AddSingleton<ProjectPokemon.Networking.Network>();
        builder.Services.AddScoped<ProjectPokemon.Middlewares.WebSocketMiddleware>();

        builder.Services.AddAuthentication()
           .AddJwtBearer(options =>
           {
               string? key = Environment.GetEnvironmentVariable("JWT_KEY");

               if (key is null)
                   throw new InvalidOperationException("JWT_KEY no definida.");

               options.TokenValidationParameters = new TokenValidationParameters()
               {
                   ValidateIssuer = false,
                   ValidateAudience = false,
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                   RoleClaimType = ClaimTypes.Role
               };

               options.Events = new JwtBearerEvents
               {
                   OnMessageReceived = context =>
                   {
                       var accessToken = context.Request.Query["access_token"];

                       if (!string.IsNullOrEmpty(accessToken))
                       {
                           context.Token = accessToken;
                       }

                       return Task.CompletedTask;
                   }
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

        app.UseHttpsRedirection();   // redirige HTTP a HTTPS
        app.UseStaticFiles();        // permite servir archivos desde wwwroot

        // WebSocket Middleware debe ir ANTES de autenticación
        app.UseWebSockets();
        app.UseMiddleware<ProjectPokemon.Middlewares.WebSocketMiddleware>();

        app.UseAuthentication();     // middleware de autenticacion
        app.UseAuthorization();      // middleware de autorizacion
        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            string? userId = context.User.FindFirst("id")?.Value;
            var userNickname = context.User.FindFirst(ClaimTypes.Name)?.Value;
            if (userId is null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            int? parsedUserId = int.TryParse(userId, out int userIdValue) ? userIdValue : null;
            var network = context.RequestServices.GetRequiredService<Network>();
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            app.Logger.LogInformation("[WS] Conectado usuario={UserId} nick={Nickname}", parsedUserId, userNickname);

            await network.ConnectAsync(socket, parsedUserId, userNickname);

            app.Logger.LogInformation("[WS] Desconectado usuario={UserId} nick={Nickname}", parsedUserId, userNickname);
        });

        app.MapControllers();        // mapea los endpoints de los controladores

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