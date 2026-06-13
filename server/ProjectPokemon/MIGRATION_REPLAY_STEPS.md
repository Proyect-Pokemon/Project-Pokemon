# Guía de Migración: Sistema de Replay por Pasos

## Resumen

El sistema de combate ahora incluye **ReplaySteps**, una nueva estructura que permite reproducir turnos paso a paso de forma determinista, reemplazando el sistema anterior de listas separadas (`Messages`, `StructuredMessages`, `Timeline`).

---

## Cambios en el Contrato

### BattleStateUpdate (Nuevo)

```json
{
  "action": "attack",
  "battle": { ...snapshot... },
  "replaySteps": [
	{
	  "stepIndex": 0,
	  "message": "Pikachu usa Impactrueno.",
	  "structuredMessage": {
		"code": "attack_used",
		"args": {
		  "actor": "pikachu",
		  "move": "impactrueno"
		}
	  },
	  "events": [
		{
		  "eventType": "attack",
		  "message": "Pikachu usa Impactrueno.",
		  "attacker": {
			"side": "player",
			"slot": 0,
			"displayName": "Pikachu"
		  },
		  "defender": {
			"side": "opponent",
			"slot": 0,
			"displayName": "Charizard"
		  },
		  "moveName": "Impactrueno",
		  "hit": true,
		  "blocked": false
		}
	  ],
	  "delayMs": null,
	  "metadata": null
	},
	{
	  "stepIndex": 1,
	  "message": "Charizard ha recibido daño.",
	  "structuredMessage": {
		"code": "damage_dealt",
		"args": {
		  "actor": "pikachu",
		  "target": "charizard",
		  "damage": 45
		}
	  },
	  "events": [
		{
		  "eventType": "hp_change",
		  "message": "Charizard pierde 45 PS.",
		  "target": {
			"side": "opponent",
			"slot": 0,
			"displayName": "Charizard"
		  },
		  "beforeHp": 120,
		  "afterHp": 75,
		  "maxHp": 150,
		  "amount": -45,
		  "cause": "move",
		  "sourceMove": "Impactrueno",
		  "sourcePokemon": {
			"side": "player",
			"slot": 0,
			"displayName": "Pikachu"
		  }
		}
	  ],
	  "delayMs": null,
	  "metadata": null
	},
	{
	  "stepIndex": 2,
	  "message": "¡Es muy eficaz!",
	  "structuredMessage": {
		"code": "super_effective",
		"args": {
		  "actor": "pikachu",
		  "target": "charizard"
		}
	  },
	  "events": [],
	  "delayMs": null,
	  "metadata": null
	}
  ],
  "messages": ["..."],  // DEPRECATED
  "structuredMessages": ["..."],  // DEPRECATED
  "timeline": ["..."],  // DEPRECATED
  "requiresSwitch": false,
  "winnerUserId": null
}
```

---

## Estructura de ReplayStep

Cada `ReplayStep` representa una **acción atómica** en el turno:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `stepIndex` | `int` | Índice de orden (0-based) |
| `message` | `string?` | Mensaje de texto legacy para compatibilidad |
| `structuredMessage` | `StructuredBattleMessage?` | Mensaje estructurado con code + args |
| `events` | `BattleEvent[]` | Lista de eventos asociados a este paso |
| `delayMs` | `int?` | Delay recomendado en ms antes del siguiente paso (null = default) |
| `metadata` | `Dictionary<string, object>?` | Metadata adicional para animaciones |

---

## Orden de Reproducción Recomendado

### Flujo de Reproducción

```typescript
async function replayTurn(replaySteps: ReplayStep[]) {
  for (const step of replaySteps) {
	// 1. Procesar mensaje si existe
	if (step.message) {
	  addMessageToLog(step.message);
	}

	// 2. Procesar eventos en orden
	for (const event of step.events) {
	  await processEvent(event);
	}

	// 3. Aplicar delay si está especificado
	const delay = step.delayMs ?? DEFAULT_STEP_DELAY;
	await sleep(delay);
  }
}

async function processEvent(event: BattleEvent) {
  switch (event.eventType) {
	case 'attack':
	  await animateAttack(event as AttackEvent);
	  break;
	case 'hp_change':
	  await animateHpChange(event as HpChangeEvent);
	  break;
	case 'switch':
	  await animateSwitch(event as SwitchEvent);
	  break;
	case 'faint':
	  await animateFaint(event as FaintEvent);
	  break;
	// ... otros tipos de eventos
  }
}
```

---

## Ejemplos de Steps Comunes

### 1. Ataque Normal (Daño)

```json
[
  {
	"stepIndex": 0,
	"message": "Pikachu usa Impactrueno.",
	"structuredMessage": { "code": "attack_used", "args": {...} },
	"events": [{ "eventType": "attack", ...}]
  },
  {
	"stepIndex": 1,
	"message": "Charizard ha recibido daño.",
	"structuredMessage": { "code": "damage_dealt", "args": {...} },
	"events": [{ "eventType": "hp_change", "amount": -45, ...}]
  }
]
```

### 2. Ataque Multi-Golpe

```json
[
  {
	"stepIndex": 0,
	"message": "Pikachu usa Doble Bofetón.",
	"events": [{ "eventType": "attack", ...}]
  },
  {
	"stepIndex": 1,
	"message": "Charizard ha recibido daño.",
	"events": [{ "eventType": "hp_change", "amount": -60, ...}]
  },
  {
	"stepIndex": 2,
	"message": "¡Golpeó 3 veces!",
	"events": []
  }
]
```

### 3. Cambio de Pokémon

```json
[
  {
	"stepIndex": 0,
	"message": "Cambio realizado: entra Bulbasaur.",
	"events": [{
	  "eventType": "switch",
	  "side": "player",
	  "previousActiveSlot": 0,
	  "newActiveSlot": 1,
	  "newPokemonName": "Bulbasaur",
	  "isAutomatic": false
	}]
  }
]
```

### 4. Pokémon Debilitado

```json
[
  {
	"stepIndex": 0,
	"message": "Charizard se debilitó.",
	"events": [{
	  "eventType": "faint",
	  "target": { "side": "opponent", "slot": 0, "displayName": "Charizard" }
	}]
  }
]
```

### 5. Efectos de Fin de Turno

```json
[
  {
	"stepIndex": 0,
	"message": "Pikachu está quemado.",
	"events": []
  },
  {
	"stepIndex": 1,
	"message": "Pikachu sufrió daño por la quemadura.",
	"events": [{
	  "eventType": "hp_change",
	  "target": {...},
	  "amount": -12,
	  "cause": "burn"
	}]
  }
]
```

---

## Perspectivas por Cliente

**Importante**: El backend **remapea automáticamente** las perspectivas (`player`/`opponent`) para cada cliente.

- **Player 1** ve sus Pokémon como `side: "player"`
- **Player 2** ve sus Pokémon como `side: "player"` (remapeado por el servidor)

**El frontend NO debe hacer ningún remapeo adicional**. Solo consumir `replaySteps` tal como llega.

---

## Migración desde Sistema Legacy

### Antes (Sistema Antiguo)

```typescript
// Frontend tenía que inferir qué mensaje corresponde a qué evento
function handleBattleUpdate(update: BattleStateUpdate) {
  // Mostrar todos los mensajes
  update.messages.forEach(msg => addLog(msg));

  // Procesar eventos SIN orden correcto ni vínculo con mensajes
  update.timeline.forEach(event => {
	if (event.eventType === 'hp_change') {
	  // ¿Qué mensaje corresponde a este cambio de HP?
	  // ¿Cuándo debería animarse?
	  updateHpBar(event.target, event.afterHp);
	}
  });
}
```

### Ahora (Sistema Nuevo)

```typescript
async function handleBattleUpdate(update: BattleStateUpdate) {
  // Reproducir steps en orden determinista
  for (const step of update.replaySteps) {
	// Mensaje y eventos ya están vinculados en el mismo step
	if (step.message) {
	  addLog(step.message);
	}

	for (const event of step.events) {
	  if (event.eventType === 'hp_change') {
		await animateHpChange(event);
	  }
	}

	await sleep(step.delayMs ?? 500);
  }
}
```

---

## Compatibilidad Transicional

Durante la Fase 1 de migración:

- `replaySteps` está **disponible y recomendado**
- `messages`, `structuredMessages`, `timeline` siguen presentes pero **marcados como obsoletos**
- Clientes legacy pueden seguir funcionando temporalmente

## Tipos de Eventos Soportados

| EventType | Clase | Propiedades Clave |
|-----------|-------|-------------------|
| `message` | `MessageEvent` | `message` |
| `attack` | `AttackEvent` | `attacker`, `defender`, `moveName`, `hit`, `blocked` |
| `hp_change` | `HpChangeEvent` | `target`, `beforeHp`, `afterHp`, `amount`, `cause` |
| `status_change` | `StatusChangeEvent` | `target`, `beforeStatus`, `afterStatus` |
| `secondary_status_change` | `SecondaryStatusChangeEvent` | `target`, `secondaryStatus`, `added` |
| `faint` | `FaintEvent` | `target` |
| `switch` | `SwitchEvent` | `side`, `previousActiveSlot`, `newActiveSlot` |
| `stat_stage_change` | `StatStageChangeEvent` | `target`, `stat`, `change`, `newStage` |
| `battle_end` | `BattleEndEvent` | `winner`, `winnerUserId` |

---

## Serialización JSON

Gracias a `JsonDerivedType`, todos los eventos se serializan correctamente con sus propiedades completas:

```json
{
  "eventType": "hp_change",
  "message": "Pikachu pierde 45 PS.",
  "target": { "side": "player", "slot": 0, "displayName": "Pikachu" },
  "beforeHp": 100,
  "afterHp": 55,
  "maxHp": 100,
  "amount": -45,
  "cause": "move",
  "sourceMove": "Impactrueno",
  "sourcePokemon": { "side": "opponent", "slot": 0, "displayName": "Raichu" }
}
```

---

## Preguntas Frecuentes

### ¿Qué pasa si un step tiene múltiples eventos?

Es válido y común. Por ejemplo, un ataque con drenaje tiene:
1. Step con `AttackEvent`
2. Step con `HpChangeEvent` (daño al defensor)
3. Step con `HpChangeEvent` (curación al atacante)

### ¿Cómo manejo animaciones concurrentes?

Si un step tiene múltiples eventos, puedes:
- **Secuencial**: Procesar eventos uno tras otro
- **Paralelo**: `await Promise.all(step.events.map(processEvent))`

Recomendamos secuencial para mejor claridad visual.

### ¿Debo usar `message` o `structuredMessage`?

- `message`: Para logs de texto simple (debugging, historial)
- `structuredMessage`: Para UI internacionalizada y estructurada

Ambos son opcionales y pueden coexistir en el mismo step.

### ¿Cuándo usar `delayMs`?

El backend puede especificar delays personalizados para mejorar el ritmo de animaciones:
- Golpes críticos: delay más largo
- Multi-hit: delays cortos entre golpes
- Cambios de Pokémon: delay estándar

Si es `null`, usa tu delay por defecto (ej: 500ms).

---

## Soporte y Feedback

Si encuentras problemas durante la migración o tienes dudas:
1. Revisa el código en `ProjectPokemon/Networking/Messages/Battle/ReplayStep.cs`
2. Consulta ejemplos en `BattleService.cs` (métodos `ExecuteSwitch`, `DecrementFieldEffects`)
3. Contacta al equipo de backend para aclaraciones

---

**Fecha de creación**: 2025-01-19  
**Versión del sistema**: v2.0 (Sistema de Replay por Pasos)
