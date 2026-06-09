using ProjectPokemon.Networking.Clients;

namespace ProjectPokemon.Networking;

public class Matchmaking {
    private readonly Queue<(Client client, int teamId)> _queue = new();
    private readonly Lock _lock = new();

    public event Action<Client, Client, int, int>? Matched;

    public bool Join(Client client, int teamId) {
        (Client client, int teamId)? partner = null;

        lock (_lock) {
            if (_queue.Any(entry => entry.client.ClientId == client.ClientId)) {
                return true;
            }

            if (_queue.Count > 0) {
                partner = _queue.Dequeue();
                partner.Value.client.Disconnected -= OnClientDisconnected;
            }
            else {
                _queue.Enqueue((client, teamId));
                client.Disconnected += OnClientDisconnected;
                return true;
            }
        }

        Matched?.Invoke(partner.Value.client, client, partner.Value.teamId, teamId);
        return false;
    }

    public bool Leave(Client client) {
        lock (_lock) {
            bool removed = false;
            Queue<(Client client, int teamId)> rest = new();

            while (_queue.Count > 0) {
                (Client queuedClient, int teamId) = _queue.Dequeue();

                if (queuedClient.ClientId == client.ClientId) {
                    removed = true;
                }
                else {
                    rest.Enqueue((queuedClient, teamId));
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

    private void OnClientDisconnected(Client client) {
        Leave(client);
    }
}
