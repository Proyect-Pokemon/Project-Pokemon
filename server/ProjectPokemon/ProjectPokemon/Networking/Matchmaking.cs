using ProjectPokemon.Networking.Clients;

namespace ProjectPokemon.Networking;

public class Matchmaking {
    private readonly Queue<(Client client, int teamId)> _queue = new();
    private readonly object _lock = new(); // object lock es reentrant-safe y compatible con todos los contextos

    // Usamos Func<Task> para que el caller pueda awaitar el handler correctamente,
    // evitando el problema de async void que silencia excepciones.
    public event Func<Client, Client, int, int, Task>? Matched;

    public bool Join(Client client, int teamId) {
        (Client client, int teamId)? partner = null;

        lock (_lock) {
            // Evitar duplicados en cola
            if (_queue.Any(entry => entry.client.ClientId == client.ClientId)) {
                return true;
            }

            if (_queue.Count > 0) {
                partner = _queue.Dequeue();
                partner.Value.client.Disconnected -= OnClientDisconnected;
            } else {
                _queue.Enqueue((client, teamId));
                client.Disconnected += OnClientDisconnected;
                return true;
            }
        }

        // Se ejecuta fuera del lock para no bloquear otros hilos durante la creación de sesión.
        // Si Matched falla, reencolar al cliente que estaba esperando para no perderlo.
        if (Matched != null) {
            _ = InvokeMatchedSafe(partner.Value.client, client, partner.Value.teamId, teamId);
        }

        return false;
    }

    public bool Leave(Client client) {
        lock (_lock) {
            bool removed = false;
            var rest = new Queue<(Client client, int teamId)>();

            while (_queue.Count > 0) {
                var entry = _queue.Dequeue();

                if (entry.client.ClientId == client.ClientId) {
                    removed = true;
                } else {
                    rest.Enqueue(entry);
                }
            }

            while (rest.Count > 0) {
                _queue.Enqueue(rest.Dequeue());
            }

            if (removed) {
                client.Disconnected -= OnClientDisconnected;
            }

            return removed;
        }
    }

    private async Task InvokeMatchedSafe(Client player1, Client player2, int team1Id, int team2Id) {
        try {
            if (Matched != null) {
                await Matched.Invoke(player1, player2, team1Id, team2Id);
            }
        }
        catch (Exception ex) {
            // Si la creación de sesión falla, reencolar a player1 para que no se pierda.
            // Player2 deberá buscar de nuevo (su conexión sigue activa).
            Console.Error.WriteLine($"[Matchmaking] Error al crear sesión: {ex.Message}");
            TryRequeue(player1, team1Id);
        }
    }

    private void TryRequeue(Client client, int teamId) {
        lock (_lock) {
            // Solo reencolar si no está ya en la cola
            if (!_queue.Any(e => e.client.ClientId == client.ClientId)) {
                _queue.Enqueue((client, teamId));
                client.Disconnected += OnClientDisconnected;
            }
        }
    }

    private void OnClientDisconnected(Client client) {
        Leave(client);
    }
}