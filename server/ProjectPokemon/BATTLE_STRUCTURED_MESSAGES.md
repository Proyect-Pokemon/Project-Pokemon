# Mensajes Estructurados de Combate - Documentación

## Objetivo

Eliminar narrativas largas con tildes del motor de combate y reemplazarlas con mensajes estructurados (códigos + argumentos) que el frontend puede interpretar sin necesidad de sanitizar texto libre.

## Contrato del Mensaje

Cada mensaje de combate incluye:

```typescript
interface StructuredBattleMessage {
  code: string;        // Código en snake_case, sin tildes
  args: { [key: string]: any };  // Datos mínimos necesarios
}
```

## Lista Completa de Códigos

### Acciones de Ataque
- `attack_used` - Pokemon usa un movimiento
- `attack_missed` - El ataque falló
- `avoided_attack` - El objetivo esquivó el ataque
- `critical_hit` - Golpe crítico
- `no_effect` - El ataque no tiene efecto
- `not_very_effective` - No muy eficaz
- `super_effective` - Súper eficaz

### Daño y Curación
- `damage_dealt` - Daño infligido
- `hp_restored` - PS recuperados
- `recoil` - Daño por retroceso
- `drain_hp` - PS drenados del oponente

### Estados Primarios
- `poisoned` - Envenenado
- `badly_poisoned` - Gravemente envenenado
- `burned` - Quemado
- `paralyzed` - Paralizado
- `asleep` - Dormido
- `frozen` - Congelado
- `status_cured` - Estado curado

### Impedimentos de Estado
- `paralyzed_cant_move` - Paralizado, no puede moverse
- `fast_asleep` - Profundamente dormido
- `frozen_solid` - Congelado sólido
- `woke_up` - Se despertó
- `thawed` - Se descongeló

### Estados Secundarios
- `confusion_start` - Confusión comienza
- `confusion_end` - Confusión termina
- `confusion_self_hit` - Se golpeó a sí mismo por confusión
- `flinched` - Retrocedió
- `seeded` - Afectado por drenadoras
- `seeded_drain` - Drenadoras drena PS
- `bound` - Atrapado
- `bound_damage` - Daño por estar atrapado

### Efectos de Fin de Turno
- `poison_damage` - Daño por veneno
- `burn_damage` - Daño por quemadura

### Cambios de Estadísticas
- `stat_rose` - Estadística subió
- `stat_fell` - Estadística bajó
- `stat_maxed` - Estadística al máximo
- `stat_minned` - Estadística al mínimo
- `stats_reset` - Todas las estadísticas reseteadas (Haze)
- `stat_protected` - Estadística protegida (por Mist)

### Efectos de Campo
- `light_screen_start` - Pantalla de Luz activada
- `light_screen_end` - Pantalla de Luz expiró
- `reflect_start` - Reflejo activado
- `reflect_end` - Reflejo expiró
- `mist_start` - Niebla activada
- `mist_end` - Niebla expiró

### Cambios de Pokémon
- `send_out` - Enviar Pokémon
- `withdraw` - Retirar Pokémon
- `forced_switch` - Cambio forzado
- `fainted` - Pokemon debilitado

### Resultado de Batalla
- `battle_won` - Batalla ganada
- `battle_lost` - Batalla perdida

### PP y Movimientos
- `out_of_pp` - Sin PP
- `no_moves_left` - Sin movimientos disponibles

## Ejemplos JSON

### Ejemplo 1: Turno Normal (Ataque Simple)

```json
{
  "action": "Attack",
  "battle": { ... },
  "messages": [
	"Pikachu usa Impactrueno. Daño: 45."
  ],
  "structuredMessages": [
	{
	  "code": "attack_used",
	  "args": {
		"actor": "pikachu",
		"owner": "player",
		"move": "impactrueno"
	  }
	},
	{
	  "code": "damage_dealt",
	  "args": {
		"actor": "pikachu",
		"actor_owner": "player",
		"target": "charmander",
		"target_owner": "opponent",
		"damage": 45
	  }
	}
  ],
  "timeline": [
	{
	  "type": "AttackEvent",
	  "attacker": { "side": "player", "slot": 0, "displayName": "Pikachu" },
	  "defender": { "side": "opponent", "slot": 0, "displayName": "Charmander" },
	  "moveName": "Impactrueno",
	  "hit": true
	},
	{
	  "type": "HpChangeEvent",
	  "target": { "side": "opponent", "slot": 0, "displayName": "Charmander" },
	  "beforeHp": 120,
	  "afterHp": 75,
	  "maxHp": 120,
	  "amount": -45
	}
  ]
}
```

### Ejemplo 2: Confusión y Auto-golpe

**IMPORTANTE**: Cuando un Pokémon está confundido y se auto-golpea:
- **NO** se emite `attack_used` 
- Se emite directamente `confusion_self_hit`

```json
{
  "action": "Attack",
  "battle": { ... },
  "messages": [
	"Pikachu está confundido y se golpeó a sí mismo por 12 PS."
  ],
  "structuredMessages": [
	{
	  "code": "confusion_self_hit",
	  "args": {
		"actor": "pikachu",
		"owner": "player",
		"damage": 12
	  }
	}
  ],
  "timeline": [
	{
	  "type": "AttackEvent",
	  "attacker": { "side": "player", "slot": 0, "displayName": "Pikachu" },
	  "defender": { "side": "player", "slot": 0, "displayName": "Pikachu" },
	  "moveName": "Impactrueno",
	  "hit": false,
	  "blocked": true,
	  "blockReason": "confused"
	},
	{
	  "type": "HpChangeEvent",
	  "target": { "side": "player", "slot": 0, "displayName": "Pikachu" },
	  "beforeHp": 100,
	  "afterHp": 88,
	  "maxHp": 120,
	  "amount": -12,
	  "cause": "confusion"
	}
  ]
}
```

### Ejemplo 3: Efecto de Campo (Light Screen)

```json
{
  "action": "Attack",
  "battle": { ... },
  "messages": [
	"Alakazam usa Pantalla de Luz.",
	"¡Pantalla de Luz reduce el daño de ataques especiales!"
  ],
  "structuredMessages": [
	{
	  "code": "attack_used",
	  "args": {
		"actor": "alakazam",
		"owner": "player",
		"move": "pantalla_de_luz"
	  }
	},
	{
	  "code": "light_screen_start",
	  "args": {
		"owner": "player",
		"turns": 5
	  }
	}
  ],
  "timeline": [
	{
	  "type": "AttackEvent",
	  "attacker": { "side": "player", "slot": 0, "displayName": "Alakazam" },
	  "defender": { "side": "opponent", "slot": 0, "displayName": "Gengar" },
	  "moveName": "Pantalla de Luz",
	  "hit": true
	},
	{
	  "type": "MessageEvent",
	  "message": "¡Pantalla de Luz reduce el daño de ataques especiales!"
	}
  ]
}
```

### Ejemplo 4: Mist Bloquea Reducción de Stats

```json
{
  "action": "Attack",
  "battle": { ... },
  "messages": [
	"Gengar usa Garra Umbría.",
	"¡La niebla protege las estadísticas de Alakazam!"
  ],
  "structuredMessages": [
	{
	  "code": "attack_used",
	  "args": {
		"actor": "gengar",
		"owner": "opponent",
		"move": "garra_umbria"
	  }
	},
	{
	  "code": "stat_protected",
	  "args": {
		"actor": "alakazam",
		"owner": "player"
	  }
	}
  ],
  "timeline": [
	{
	  "type": "AttackEvent",
	  "attacker": { "side": "opponent", "slot": 0, "displayName": "Gengar" },
	  "defender": { "side": "player", "slot": 0, "displayName": "Alakazam" },
	  "moveName": "Garra Umbría",
	  "hit": true
	},
	{
	  "type": "MessageEvent",
	  "message": "¡La niebla protege las estadísticas de Alakazam!"
	}
  ]
}
```

### Ejemplo 5: Cambio de Stats

```json
{
  "structuredMessages": [
	{
	  "code": "attack_used",
	  "args": {
		"actor": "machamp",
		"owner": "player",
		"move": "danza_espada"
	  }
	},
	{
	  "code": "stat_rose",
	  "args": {
		"actor": "machamp",
		"owner": "player",
		"stat": "attack",
		"stages": 2
	  }
	}
  ]
}
```

## Reglas de Flujo Importantes

### 1. Confusión y Auto-golpe
- Si está confundido y se auto-golpea: **NO** emitir `attack_used`
- Emitir solo `confusion_self_hit`

### 2. Nombres Normalizados
- Todos los nombres de Pokémon en minúsculas, sin tildes
- Ejemplos: `"pikachu"`, `"farfetchd"`, `"nidoran_f"`

### 3. Nombres de Movimientos
- En snake_case, sin tildes
- Ejemplos: `"impactrueno"`, `"pantalla_de_luz"`, `"danza_espada"`

### 4. Owners
- Solo dos valores posibles: `"player"` o `"opponent"`
- Relativo a la perspectiva del usuario que recibe el mensaje

## Compatibilidad

### Mantenido (Timeline Técnico)
- `hp_change` - Para animaciones de barras de vida
- `status_change` - Para iconos de estado
- `switch` - Para animaciones de cambio
- `faint` - Para animaciones de derrota
- `stat_stage_change` - Para indicadores de stats

### Nuevo (Chat/Mensajes)
- `structuredMessages` - Lista de mensajes estructurados
- Reemplaza el array `messages` (legacy, mantenido temporalmente)

### Deprecado
- `messages` (array de strings) - Mantenido para compatibilidad temporal
- El frontend debe migrar a usar `structuredMessages`

## Nota de Migración para Frontend

1. **Leer `structuredMessages` en lugar de `messages`**
2. **Mapear códigos a traducciones locales**
3. **Usar `args` para interpolar nombres/valores**
4. **Mantener `timeline` para animaciones**

Ejemplo de mapeo en frontend:

```typescript
const messageTemplates = {
  'attack_used': (args) => `${args.actor} usa ${args.move}`,
  'damage_dealt': (args) => `${args.target} recibe ${args.damage} de daño`,
  'confusion_self_hit': (args) => `${args.actor} se golpea a sí mismo (${args.damage} PS)`,
  // ... más mapeos
};

function renderMessage(msg: StructuredBattleMessage): string {
  const template = messageTemplates[msg.code];
  return template ? template(msg.args) : msg.code;
}
```

## Beneficios

✅ Sin tildes - Compatible con cualquier encoding
✅ Sin narrativas largas - Flexible para i18n
✅ Estructurado - Type-safe en TypeScript
✅ Extensible - Fácil añadir nuevos códigos
✅ Testeable - Assertions simples por código
✅ Localizable - Frontend decide el idioma/formato

## Implementación Backend

### Archivos Creados
1. `BattleMessageCode.cs` - Constantes de códigos
2. `StructuredBattleMessage.cs` - Clase del mensaje
3. `TextNormalizer.cs` - Utilidad para remover tildes

### Archivos Modificados
1. `BattleMessage.cs` - Añadido `StructuredMessages`
2. `BattleService.cs` - Generación de mensajes estructurados
3. `Network.cs` - Envío de mensajes estructurados
