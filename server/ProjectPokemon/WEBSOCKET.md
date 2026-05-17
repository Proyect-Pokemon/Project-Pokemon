# Sistema WebSocket Real-Time

## Tabla de Contenidos
1. [Filosofía del Sistema](#filosofía-del-sistema)
2. [Arquitectura](#arquitectura)
3. [Flujo de Comunicación](#flujo-de-comunicación)
4. [Tipos de Mensajes](#tipos-de-mensajes)
5. [Ejemplos de Uso](#ejemplos-de-uso)
6. [Implementación Frontend](#implementación-frontend)
7. [Cambios de Arquitectura](#cambios-de-arquitectura)

---

## Filosofía del Sistema

**Principio:** Una vez que el usuario se autentica y conecta por WebSocket, TODA la comunicación es en tiempo real por WebSocket. No se mezcla HTTP con WebSocket.

```
Flujo Correcto:
1. Usuario → Login (HTTP) → JWT Token
2. Usuario → Conecta WebSocket (ws://...?token=JWT)
3. Usuario → Envía mensajes (WebSocket)
4. Servidor → Responde mensajes (WebSocket)
```

**BattleController está marcado como obsoleto** - Las batallas se inician por WebSocket.

---

## Arquitectura

### Estructura del Proyecto

```
ProjectPokemon/
├── Networking/
│   ├── Clients/
│   │   ├── Client.cs                    Cliente WebSocket genérico
│   │   └── WebSocketHandler.cs          Handler de mensajes
│   ├── Messages/
│   │   ├── IMessage.cs                  Interfaz base
│   │   ├── MessageType.cs               Battle, Chat, System, Lobby
│   │   ├── MessageSerializer.cs         Serialización JSON
│   │   ├── Battle/
│   │   │   ├── BattleAction.cs          StartBattle, Attack, Switch, Forfeit
│   │   │   ├── BattleMessage.cs         Requests/Responses
│   │   │   └── ChatMessage.cs           Chat en batalla
│   │   └── Lobby/
│   │       ├── LobbyAction.cs           JoinLobby, GetFriends
│   │       └── LobbyMessage.cs          Lobby requests/responses
│   └── Network.cs                       Router central de mensajes
├── Services/
│   ├── BattleService.cs                 Lógica de negocio batallas
│   └── BattleSessionManager.cs          Gestión de sesiones
├── Models/Battle/
│   ├── BattleSession.cs                 Sesión de batalla
│   ├── BattleSide.cs                    Lado de la batalla
│   └── PokemonBattle.cs                 Pokemon en combate
├── Controllers/
│   ├── WebSocketController.cs           Endpoint WebSocket
│   └── BattleController.cs              [OBSOLETO]
└── Middlewares/
    └── WebSocketMiddleware.cs           Autenticación JWT
```

### Componente: Network.cs

El componente central que gestiona todas las conexiones y enruta mensajes:

```csharp
public class Network {
    private IDictionary<Guid, Client> _clients;
    private IDictionary<string, HashSet<Guid>> _battleClients; // battleId -> clientIds

    public Task ConnectAsync(WebSocket webSocket);
    public void JoinBattle(Guid clientId, string battleId);
    public Task BroadcastToBattleAsync<T>(string battleId, T message);
}
```

**Ventajas:**
- Un cliente puede estar en múltiples contextos simultáneamente
- Broadcast eficiente a grupos de clientes
- Fácil agregar nuevos contextos (lobby, trade, etc.)

### Cambio: BattleClient → Client

**Antes:**
```csharp
public class BattleClient {
    public string? BattleId { get; set; }  // Solo 1 batalla
}
```

**Ahora:**
```csharp
public class Client {
    public Guid ClientId { get; }
    public int? UserId { get; set; }
    // Sin propiedades de contexto específico
    // Network gestiona las relaciones cliente-contexto
}
```

**Motivo:** Un cliente puede participar en batallas, chat, lobby, etc. simultáneamente.

---

## Flujo de Comunicación

### 1. Autenticación (HTTP - Una vez)
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "ash",
  "password": "pikachu123"
}
```

**Respuesta:**
```json
{
  "token": "eyJhbGc...",
  "userId": 1,
  "username": "ash"
}
```

### 2. Conexión WebSocket
```javascript
const token = localStorage.getItem('jwt');
const ws = new WebSocket(`ws://localhost:5000/websocket?token=${token}`);

ws.onopen = () => {
  console.log('Conectado al servidor');
};

ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  handleMessage(message);
};
```

### 3. Unirse al Lobby
```json
// Cliente → Servidor
{
  "type": 4,
  "action": 1
}
```

**Respuesta:**
```json
{
  "type": 4,
  "action": 1,
  "username": "ash",
  "onlineFriends": [
    { "userId": 2, "username": "misty", "status": "online" }
  ]
}
```

### 4. Iniciar Batalla
```json
// Cliente → Servidor
{
  "type": 1,
  "action": 0,
  "teamId": 5,
  "opponentUserId": null
}
```

**Respuesta:**
```json
{
  "type": 1,
  "action": 0,
  "battleId": "abc-123-def",
  "success": true,
  "initialState": {
    "battleId": "abc-123-def",
    "turn": 1,
    "playerSide": {
      "team": [/* 6 pokemon */],
      "activeSlot": 0
    },
    "opponentSide": {
      "team": [/* 6 pokemon */],
      "activeSlot": 0
    }
  }
}
```

### 5. Atacar
```json
// Cliente → Servidor
{
  "type": 1,
  "action": 1,
  "battleId": "abc-123-def",
  "moveName": "Thunderbolt"
}
```

**Respuesta (Broadcast a ambos jugadores):**
```json
{
  "type": 1,
  "action": 1,
  "battle": {
    "battleId": "abc-123-def",
    "turn": 2,
    "playerSide": { /* estado actualizado */ },
    "opponentSide": { /* estado actualizado */ }
  },
  "messages": [
    "Pikachu usó Thunderbolt!",
    "Es muy eficaz!",
    "Charizard perdió 45 HP!"
  ],
  "requiresSwitch": false,
  "winnerSide": null
}
```

### 6. Chat Durante Batalla
```json
// Cliente → Servidor
{
  "type": 2,
  "battleId": "abc-123-def",
  "content": "Buena suerte!",
  "senderName": "Ash"
}
```

**Respuesta (Broadcast):**
```json
{
  "type": 2,
  "battleId": "abc-123-def",
  "content": "Buena suerte!",
  "senderName": "Ash",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

---

## Tipos de Mensajes

### MessageType (enum)
| Valor | Tipo | Descripción |
|-------|------|-------------|
| 1 | Battle | Acciones y estados de batalla |
| 2 | Chat | Mensajes de chat |
| 3 | System | Notificaciones del sistema |
| 4 | Lobby | Lobby y lista de amigos |

### BattleAction (enum)
| Valor | Acción | Descripción |
|-------|--------|-------------|
| 0 | StartBattle | Iniciar nueva batalla |
| 1 | Attack | Usar un movimiento |
| 2 | Switch | Cambiar Pokemon |
| 3 | Forfeit | Rendirse |

### LobbyAction (enum)
| Valor | Acción | Descripción |
|-------|--------|-------------|
| 1 | JoinLobby | Unirse al lobby |
| 2 | LeaveLobby | Salir del lobby |
| 3 | GetOnlineFriends | Obtener amigos online |
| 4 | SendFriendRequest | Enviar solicitud de amistad |

---

## Ejemplos de Uso

### Backend: Enviar Mensaje a Todos en una Batalla

```csharp
// En Network.cs
var update = new BattleStateUpdate {
    Action = BattleAction.Attack,
    Battle = CreateBattleSnapshot(battle),
    Messages = new List<string> { "Pikachu usó Thunderbolt!" },
    RequiresSwitch = false
};

await BroadcastToBattleAsync(battleId, update);
```

### Backend: Manejar Acción de Batalla

```csharp
private async Task HandleBattleAction(Client client, BattleActionRequest request) {
    var battle = _sessionManager.GetBattle(request.BattleId);

    // Procesar la acción
    battle.BattleLog.Add($"Acción recibida: {request.Action}");

    // Broadcast a todos los clientes de la batalla
    var update = new BattleStateUpdate { /* ... */ };
    await BroadcastToBattleAsync(request.BattleId, update);
}
```

---

## Implementación Frontend

### Clase WebSocket Service

```typescript
class WebSocketService {
  private ws: WebSocket;
  private messageHandlers = new Map<MessageType, Function[]>();

  connect(token: string) {
    this.ws = new WebSocket(`ws://localhost:5000/websocket?token=${token}`);

    this.ws.onopen = () => this.joinLobby();
    this.ws.onmessage = (e) => this.handleMessage(JSON.parse(e.data));
  }

  send(message: any) {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    }
  }

  on(type: MessageType, handler: Function) {
    if (!this.messageHandlers.has(type)) {
      this.messageHandlers.set(type, []);
    }
    this.messageHandlers.get(type)!.push(handler);
  }

  private handleMessage(message: any) {
    const handlers = this.messageHandlers.get(message.type);
    if (handlers) {
      handlers.forEach(h => h(message));
    }
  }

  joinLobby() {
    this.send({ type: 4, action: 1 });
  }

  startBattle(teamId: number) {
    this.send({ type: 1, action: 0, teamId });
  }

  attack(battleId: string, moveName: string) {
    this.send({ type: 1, action: 1, battleId, moveName });
  }

  switchPokemon(battleId: string, targetSlot: number) {
    this.send({ type: 1, action: 2, battleId, targetSlot });
  }

  forfeit(battleId: string) {
    this.send({ type: 1, action: 3, battleId });
  }

  sendChat(battleId: string, content: string) {
    this.send({ type: 2, battleId, content });
  }
}
```

### Uso en Componente

```typescript
const ws = new WebSocketService();

// Conectar
ws.connect(localStorage.getItem('jwt')!);

// Escuchar mensajes de batalla
ws.on(MessageType.Battle, (message) => {
  if (message.action === 0) {
    console.log('Batalla iniciada:', message.battleId);
    setBattle(message.initialState);
  } else {
    console.log('Actualización de batalla');
    updateBattle(message.battle);
    showMessages(message.messages);
  }
});

// Escuchar chat
ws.on(MessageType.Chat, (message) => {
  console.log(`${message.senderName}: ${message.content}`);
  addChatMessage(message);
});

// Iniciar batalla
function handleStartBattle() {
  ws.startBattle(selectedTeamId);
}

// Atacar
function handleAttack(moveName: string) {
  ws.attack(currentBattleId, moveName);
}

// Cambiar Pokemon
function handleSwitch(slot: number) {
  ws.switchPokemon(currentBattleId, slot);
}

// Rendirse
function handleForfeit() {
  ws.forfeit(currentBattleId);
}

// Enviar chat
function handleSendChat() {
  ws.sendChat(currentBattleId, chatInput);
}
```

---

## Cambios de Arquitectura

### Antes vs Ahora

**Antes:**
```
Cliente WebSocket (BattleClient)
├── ClientId: Guid
├── UserId: int?
└── BattleId: string?  <- Solo UNA batalla
```

**Ahora:**
```
Cliente WebSocket (Client)
├── ClientId: Guid
└── UserId: int?

Network (gestiona relaciones)
├── _clients: Dictionary<Guid, Client>
├── _battleClients: Dictionary<string, HashSet<Guid>>
└── Futuro: _chatRooms, _lobbies, etc.
```

### Sistema de Broadcast

**Ahora:**
```csharp
// Broadcast a TODOS los clientes de la batalla
await BroadcastToBattleAsync(battleId, update);
```

Esto permite que ambos jugadores reciban actualizaciones simultáneas en tiempo real.

---

## TODO Pendiente

1. **Autenticación JWT en WebSocket**
   - Extraer `userId` del token en `WebSocketMiddleware`
   - Asignar `client.UserId` al conectarse

2. **Integrar BattleService completo**
   - Crear batalla real en `HandleStartBattle`
   - Conectar con base de datos

3. **Implementar PvP**
   - Matchmaking entre jugadores
   - Gestión de salas para 2 jugadores

4. **Sistema de lobby completo**
   - Lista de amigos online
   - Invitaciones a batalla

5. **Chat global**
   - Chat fuera de batallas
   - Sistema de notificaciones

6. **Reconexión automática**
   - Detectar desconexiones
   - Reconectar y restaurar estado

---