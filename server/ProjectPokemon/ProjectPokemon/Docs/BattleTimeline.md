# Timeline de Eventos de Batalla - Documentación

## Resumen

El servidor ahora envía un único mensaje `BattleStateUpdate` por turno que incluye:

1. **`Messages`** (List<string>): Textos legacy para compatibilidad
2. **`Timeline`** (List<BattleEvent>): Lista ordenada de eventos estructurados
3. **`Battle`** (BattleSnapshot): Estado final del turno

## Tipos de Eventos

### 1. `message`
Mensaje genérico sin cambio de estado.
```json
{
  "eventType": "message",
  "message": "Texto del mensaje"
}
```

### 2. `attack`
Intento de ataque de un Pokémon.
```json
{
  "eventType": "attack",
  "message": "Pikachu usa Impactrueno. Daño: 35.",
  "attacker": {
	"side": "player",
	"slot": 0,
	"displayName": "Pikachu"
  },
  "defender": {
	"side": "opponent",
	"slot": 0,
	"displayName": "Charmander"
  },
  "moveName": "Impactrueno",
  "hit": true,
  "blocked": false,
  "blockReason": null
}
```

Si el ataque fue bloqueado:
```json
{
  "eventType": "attack",
  "message": "Pikachu está paralizado y no puede moverse.",
  "attacker": { "side": "player", "slot": 0, "displayName": "Pikachu" },
  "defender": { "side": "opponent", "slot": 0, "displayName": "Charmander" },
  "moveName": "Impactrueno",
  "hit": false,
  "blocked": true,
  "blockReason": "paralyzed"
}
```

**Posibles `blockReason`**: `"frozen"`, `"asleep"`, `"paralyzed"`, `"confused"`

### 3. `hp_change`
Cambio de puntos de salud.
```json
{
  "eventType": "hp_change",
  "message": "Charmander pierde 35 PS.",
  "target": {
	"side": "opponent",
	"slot": 0,
	"displayName": "Charmander"
  },
  "beforeHp": 100,
  "afterHp": 65,
  "maxHp": 120,
  "amount": -35,
  "cause": "move",
  "sourceMove": "Impactrueno",
  "sourcePokemon": {
	"side": "player",
	"slot": 0,
	"displayName": "Pikachu"
  }
}
```

**Posibles `cause`**:
- `"move"`: Daño por movimiento
- `"burn"`: Daño por quemadura
- `"poison"`: Daño por envenenamiento
- `"badly_poisoned"`: Daño por envenenamiento grave
- `"leech_seed"`: Daño/curación por drenadoras
- `"confusion_self_hit"`: Daño por golpearse a sí mismo por confusión
- `"status_effect"`: Otros efectos de estado

### 4. `status_change`
Cambio de estado primario.
```json
{
  "eventType": "status_change",
  "message": "Pikachu se ha despertado y puede atacar.",
  "target": {
	"side": "player",
	"slot": 0,
	"displayName": "Pikachu"
  },
  "beforeStatus": "Sleep",
  "afterStatus": "None",
  "cause": "woke_up"
}
```

**Posibles `cause`**: `"move"`, `"thawed"`, `"woke_up"`

### 5. `secondary_status_change`
Cambio de estados secundarios.
```json
{
  "eventType": "secondary_status_change",
  "message": "Pikachu ya no está confundido.",
  "target": {
	"side": "player",
	"slot": 0,
	"displayName": "Pikachu"
  },
  "secondaryStatus": "Confuse",
  "added": false
}
```

### 6. `faint`
Pokémon debilitado.
```json
{
  "eventType": "faint",
  "message": "Charmander se debilitó.",
  "target": {
	"side": "opponent",
	"slot": 0,
	"displayName": "Charmander"
  }
}
```

### 7. `switch`
Cambio de Pokémon activo.
```json
{
  "eventType": "switch",
  "message": "Entra Squirtle automáticamente.",
  "side": "opponent",
  "previousActiveSlot": 0,
  "newActiveSlot": 1,
  "newPokemonName": "Squirtle",
  "isAutomatic": true
}
```

`isAutomatic`: 
- `true` = cambio por KO
- `false` = acción del jugador

### 8. `stat_stage_change`
Cambio de stages de estadísticas.
```json
{
  "eventType": "stat_stage_change",
  "message": "El ataque de Pikachu subió.",
  "target": {
	"side": "player",
	"slot": 0,
	"displayName": "Pikachu"
  },
  "stat": "Attack",
  "change": 1,
  "newStage": 1
}
```

### 9. `battle_end`
Fin de la batalla.
```json
{
  "eventType": "battle_end",
  "message": "Jugador ganó la batalla.",
  "winner": "player",
  "winnerUserId": 123
}
```

## Ejemplo Completo de Turno

Escenario: Pikachu ataca a Charmander, lo derrota, entra Squirtle automáticamente, y luego se aplica daño de quemadura a Pikachu.

```json
{
  "action": "Attack",
  "battle": {
	"battleId": "550e8400-e29b-41d4-a716-446655440000",
	"turn": 3,
	"playerSide": { ... },
	"opponentSide": { ... }
  },
  "messages": [
	"Pikachu usa Impactrueno. Daño: 55.",
	"Charmander se debilitó.",
	"Entra Squirtle automáticamente.",
	"Pikachu sufre daño por quemadura (7 PS)."
  ],
  "timeline": [
	{
	  "eventType": "attack",
	  "message": "Pikachu usa Impactrueno. Daño: 55.",
	  "attacker": {
		"side": "player",
		"slot": 0,
		"displayName": "Pikachu"
	  },
	  "defender": {
		"side": "opponent",
		"slot": 0,
		"displayName": "Charmander"
	  },
	  "moveName": "Impactrueno",
	  "hit": true,
	  "blocked": false,
	  "blockReason": null
	},
	{
	  "eventType": "hp_change",
	  "message": "Charmander pierde 55 PS.",
	  "target": {
		"side": "opponent",
		"slot": 0,
		"displayName": "Charmander"
	  },
	  "beforeHp": 55,
	  "afterHp": 0,
	  "maxHp": 120,
	  "amount": -55,
	  "cause": "move",
	  "sourceMove": "Impactrueno",
	  "sourcePokemon": {
		"side": "player",
		"slot": 0,
		"displayName": "Pikachu"
	  }
	},
	{
	  "eventType": "faint",
	  "message": "Charmander se debilitó.",
	  "target": {
		"side": "opponent",
		"slot": 0,
		"displayName": "Charmander"
	  }
	},
	{
	  "eventType": "switch",
	  "message": "Entra Squirtle automáticamente.",
	  "side": "opponent",
	  "previousActiveSlot": 0,
	  "newActiveSlot": 1,
	  "newPokemonName": "Squirtle",
	  "isAutomatic": true
	},
	{
	  "eventType": "hp_change",
	  "message": "Pikachu sufre daño por quemadura (7 PS).",
	  "target": {
		"side": "player",
		"slot": 0,
		"displayName": "Pikachu"
	  },
	  "beforeHp": 85,
	  "afterHp": 78,
	  "maxHp": 110,
	  "amount": -7,
	  "cause": "burn",
	  "sourceMove": null,
	  "sourcePokemon": null
	}
  ],
  "requiresSwitch": false,
  "winnerUserId": null
}
```

## Escenario: Leech Seed (Drenadoras)

Cuando un Pokémon con Leech Seed pierde PS al final del turno:

```json
{
  "eventType": "hp_change",
  "message": "Bulbasaur pierde 15 PS por Drenadoras.",
  "target": {
	"side": "opponent",
	"slot": 0,
	"displayName": "Bulbasaur"
  },
  "beforeHp": 80,
  "afterHp": 65,
  "maxHp": 120,
  "amount": -15,
  "cause": "leech_seed",
  "sourcePokemon": {
	"side": "player",
	"slot": 0,
	"displayName": "Venusaur"
  }
}
```

Y el evento de curación para el Pokémon que aplicó Leech Seed:

```json
{
  "eventType": "hp_change",
  "message": "Venusaur recupera 15 PS.",
  "target": {
	"side": "player",
	"slot": 0,
	"displayName": "Venusaur"
  },
  "beforeHp": 85,
  "afterHp": 100,
  "maxHp": 150,
  "amount": 15,
  "cause": "leech_seed",
  "sourcePokemon": {
	"side": "opponent",
	"slot": 0,
	"displayName": "Bulbasaur"
  }
}
```

## Uso en el Frontend

El frontend puede:

1. **Ignorar `messages`** si prefiere construir sus propios textos
2. **Recorrer `timeline`** en orden para reproducir cada evento:
   - Animar ataques
   - Actualizar barras de HP con valores exactos
   - Mostrar/ocultar iconos de estado
   - Animar cambios de Pokémon
   - Mostrar efectos visuales por causa (`burn`, `poison`, etc.)

3. **Usar `battle` snapshot** como estado final verificado después de todos los eventos

## Compatibilidad

- **`messages`** se mantiene para compatibilidad con frontends legacy
- **`timeline`** es la nueva fuente de verdad para reproducción precisa
- El orden de eventos en `timeline` refleja exactamente la secuencia de ejecución en el motor de batalla
