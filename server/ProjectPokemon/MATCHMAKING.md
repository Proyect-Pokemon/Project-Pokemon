# Sistema de Matchmaking - Combate PvP

## Flujo Completo del Usuario

### 1. Login y Conexión WebSocket
```javascript
// 1. Login HTTP
const response = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ username: 'ash', password: 'pikachu123' })
});
const { token, userId } = await response.json();

// 2. Conectar WebSocket
const ws = new WebSocket(`ws://localhost:5000/websocket?token=${token}`);

ws.onopen = () => {
  console.log('Conectado al servidor');

  // 3. Unirse al lobby automáticamente
  ws.send(JSON.stringify({
    type: 4,  // MessageType.Lobby
    action: 1 // LobbyAction.JoinLobby
  }));
};
```

### 2. Buscar Combate (Matchmaking)
```javascript
// Usuario selecciona equipo y busca combate
function searchBattle(teamId) {
  ws.send(JSON.stringify({
    type: 4,           // MessageType.Lobby
    action: 5,         // LobbyAction.SearchBattle
    teamId: teamId
  }));

  // Mostrar UI de "Buscando rival..."
  showSearchingUI();
}
```

### 3. Recibir Respuesta de Búsqueda
```javascript
ws.onmessage = (event) => {
  const message = JSON.parse(event.data);

  switch (message.type) {
    case 4: // Lobby
      if (message.action === 5) { // SearchBattle
        if ('battleId' in message) {
          // Se encontró rival!
          console.log('Rival encontrado:', message.opponentUsername);
          console.log('Battle ID:', message.battleId);

          // Ocultar UI de búsqueda
          hideSearchingUI();

          // Mostrar pantalla de batalla
          startBattleScreen(message.battleId, message.opponentUsername);
        } else {
          // Confirmación de que está buscando
          console.log(message.message); // "Buscando rival..."
        }
      }
      break;

    case 1: // Battle
      if (message.action === 0) { // StartBattle
        // Estado inicial de la batalla
        console.log('Estado inicial recibido');
        renderBattle(message.battle);
        showMessages(message.messages);
      } else {
        // Actualización de batalla
        updateBattle(message.battle);
        showMessages(message.messages);
      }
      break;

    case 2: // Chat
      addChatMessage(message.senderName, message.content);
      break;
  }
};
```

### 4. Cancelar Búsqueda (Opcional)
```javascript
function cancelSearch() {
  ws.send(JSON.stringify({
    type: 4,           // MessageType.Lobby
    action: 6          // LobbyAction.CancelSearch
  }));

  hideSearchingUI();
}
```

### 5. Durante la Batalla: Atacar
```javascript
function attack(battleId, moveName) {
  ws.send(JSON.stringify({
    type: 1,              // MessageType.Battle
    action: 1,            // BattleAction.Attack
    battleId: battleId,
    moveName: moveName
  }));
}
```

### 6. Durante la Batalla: Cambiar Pokemon
```javascript
function switchPokemon(battleId, targetSlot) {
  ws.send(JSON.stringify({
    type: 1,              // MessageType.Battle
    action: 2,            // BattleAction.Switch
    battleId: battleId,
    targetSlot: targetSlot // 0-5
  }));
}
```

### 7. Durante la Batalla: Chat
```javascript
function sendChatMessage(battleId, content) {
  ws.send(JSON.stringify({
    type: 2,              // MessageType.Chat
    battleId: battleId,
    content: content,
    senderName: localStorage.getItem('username')
  }));
}
```

### 8. Rendirse
```javascript
function forfeit(battleId) {
  ws.send(JSON.stringify({
    type: 1,              // MessageType.Battle
    action: 3,            // BattleAction.Forfeit
    battleId: battleId
  }));
}
```

---

## Flujo Backend (Ya Implementado)

### 1. Cliente busca combate
```
Cliente 1 → SearchBattleRequest (teamId: 5)
    ↓
Network.HandleSearchBattle()
    ↓
Matchmaking.Join() → Cola vacía
    ↓
Cliente 1 añadido a la cola
    ↓
Respuesta: "Buscando rival..."
```

### 2. Segundo cliente busca combate (EMPAREJAMIENTO)
```
Cliente 2 → SearchBattleRequest (teamId: 3)
    ↓
Network.HandleSearchBattle()
    ↓
Matchmaking.Join() → Cola tiene 1 jugador
    ↓
Matchmaking saca Cliente 1 de la cola
    ↓
Evento Matched(Cliente1, Cliente2, team1Id, team2Id)
    ↓
Network.OnPlayersMatched()
    ↓
BattleService.StartBattleAsync() → Crea BattleSession
    ↓
JoinBattle(cliente1, battleId)
JoinBattle(cliente2, battleId)
    ↓
Enviar BattleMatchedNotification a ambos
    ↓
Broadcast BattleStateUpdate inicial
```

### 3. Durante batalla: Acción de jugador
```
Cliente 1 → BattleActionRequest (attack, "Thunderbolt")
    ↓
Network.HandleBattleAction()
    ↓
Procesar acción (TODO: implementar lógica)
    ↓
BroadcastToBattleAsync() → Envía a AMBOS clientes
```

### 4. Chat durante batalla
```
Cliente 1 → ChatMessage ("GG!")
    ↓
Network.HandleChatMessage()
    ↓
BroadcastToBattleAsync() → Ambos clientes reciben
```

---

## Componentes Implementados

### 1. Matchmaking.cs
- Cola FIFO (First In First Out)
- Emparejamiento instantáneo cuando hay 2 jugadores
- Thread-safe con locks
- Evento `Matched` cuando se emparejan jugadores

### 2. Network.cs
- `HandleSearchBattle()` - Añade jugador a cola
- `HandleCancelSearch()` - Elimina jugador de cola
- `OnPlayersMatched()` - Crea batalla y notifica a ambos
- Limpia cola al desconectar

### 3. LobbyMessage.cs
- `SearchBattleRequest` - Cliente solicita buscar
- `SearchBattleResponse` - Servidor confirma búsqueda
- `CancelSearchRequest` - Cliente cancela
- `CancelSearchResponse` - Servidor confirma cancelación
- `BattleMatchedNotification` - Servidor notifica rival encontrado

---

## Ventajas del Sistema

1. **Matchmaking Automático**: No requiere invitaciones, empareja automáticamente
2. **FIFO**: Orden de llegada, justo para todos
3. **Cancelable**: Usuario puede cancelar mientras busca
4. **Salas de Batalla**: Cada batalla tiene su sala privada
5. **Chat Integrado**: Chat automático en cada batalla
6. **Broadcast**: Ambos jugadores reciben actualizaciones simultáneas
7. **Desconexión**: Limpia automáticamente de la cola

---

## Secuencia Completa (Ejemplo)

```
T=0s:  Ash busca combate (teamId: 5)
       → Server: "Buscando rival..."
       → Ash en cola de espera

T=3s:  Misty busca combate (teamId: 3)
       → Server: Empareja Ash + Misty
       → Crea BattleSession (battleId: "abc-123")
       → Ash recibe: BattleMatchedNotification (rival: Misty)
       → Misty recibe: BattleMatchedNotification (rival: Ash)

T=3.1s: Ambos reciben estado inicial de batalla
       → BattleStateUpdate (turn: 1, pokemon activos, etc.)

T=5s:  Ash ataca con "Thunderbolt"
       → Server procesa
       → Broadcast a Ash y Misty: "Pikachu usó Thunderbolt!"

T=7s:  Misty envía chat: "Nice move!"
       → Broadcast a Ash y Misty

T=10s: Misty ataca con "Water Gun"
       → Server procesa
       → Broadcast a ambos

T=15s: Ash se rinde (Forfeit)
       → Server marca a Misty como ganadora
       → Broadcast a ambos: "Misty wins!"
```

---

## TODO Pendiente

1. **Validación de equipos**: Verificar que teamId existe y pertenece al usuario
2. **Lógica de combate**: Implementar procesamiento de ataques/switch
3. **IA para equipos rivales**: Cargar equipo real del jugador 2
4. **Timeouts**: Límite de tiempo para buscar rival
5. **Ranking/ELO**: Sistema de emparejamiento por nivel
6. **Reconexión**: Permitir reconectar a batalla en progreso
7. **Notificaciones**: Notificar cuando el rival se desconecta

---

## Preguntas Frecuentes

**¿Qué pasa si un jugador se desconecta mientras busca?**
- Se elimina automáticamente de la cola en `OnClientDisconnected()`

**¿Qué pasa si un jugador se desconecta durante la batalla?**
- TODO: Implementar lógica de victoria automática para el otro jugador

**¿Puedo buscar combate con un amigo específico?**
- Actualmente no, solo matchmaking aleatorio
- TODO: Implementar invitaciones directas

**¿Cuánto tiempo espero en cola?**
- Instantáneo si hay otro jugador esperando
- Indefinido si no hay nadie (TODO: agregar timeout)

**¿Puedo estar en múltiples batallas a la vez?**
- Técnicamente sí (el sistema lo soporta)
- TODO: Decidir si permitirlo o no
