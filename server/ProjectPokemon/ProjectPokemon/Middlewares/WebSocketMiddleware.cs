using Microsoft.Extensions.Primitives;

namespace ProjectPokemon.Middlewares;

public class WebSocketMiddleware : IMiddleware {
    private const string TOKEN_KEY = "token";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        if (context.WebSockets.IsWebSocketRequest) {
            // Solo la primera petición de un websocket usa el método GET,
            // las posteriores usan CONNECT, por tanto hay que convertirlo en GET
            if (context.Request.Method == "CONNECT") {
                context.Request.Method = "GET";
            }

            if (context.Request.Query.TryGetValue(TOKEN_KEY, out StringValues jwt)) {
                context.Request.Headers.Authorization = $"Bearer {jwt}";
            }
        }

        await next(context);
    }
}
