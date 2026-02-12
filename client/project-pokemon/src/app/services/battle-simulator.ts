import { PokemonApi } from "../models/pokemon-api";
import { Move as Movement } from "../models/move";
import { CalculateDamageService } from "./calculate-damage";

// Simula el combate por consola
export function simulateBattle(
    pokemonA: PokemonApi,
    pokemonB: PokemonApi,
    movementA: Movement,
    hpA: number,
    hpB: number
) {
    // Movimiento del usuario (pokemonA)
    const userMovement = movementA;
    // Movimiento del oponente (pokemonB)
    const opponentMovement = pokemonB.moves[Math.floor(Math.random() * pokemonB.moves.length)];

    // Variables para calcular el daño:
    // USUARIO
    const stabA = userMovement.type === pokemonA.type1 || userMovement.type === pokemonA.type2 ? 1.5 : 1;
    const effectiveA = 1;
    const variationA = Math.floor(Math.random() * (100 - 85 + 1)) + 85; // Valor máximo de 100 y mínimo de 85, +1 para incluir ambos
    const attackersLevel = 50;
    const attackA = userMovement.moveClass === "Physical" ? pokemonA.atk : pokemonA.spa;
    const powerA = userMovement.power;
    const defenseA = opponentMovement.moveClass === "Physical" ? pokemonB.def : pokemonB.spd;

    // OPONENTE
    const stabB = opponentMovement.type === pokemonB.type1 || opponentMovement.type === pokemonB.type2 ? 1.5 : 1;
    const effectiveB = 1;
    const variationB = Math.floor(Math.random() * (100 - 85 + 1)) + 85;
    const attackB = opponentMovement.moveClass === "Physical" ? pokemonB.atk : pokemonB.spa;
    const powerB = opponentMovement.power;
    const defenseB = userMovement.moveClass === "Physical" ? pokemonA.def : pokemonA.spd;

    // Se llama al servicio calculateDamage
    const damageService = new CalculateDamageService();
    const damageA = damageService.calculateDamage(stabA, effectiveA, variationA, attackA, powerA ?? 0, defenseA);
    const damageB = damageService.calculateDamage(stabB, effectiveB, variationB, attackB, powerB ?? 0, defenseB);

    // Restar el daño a la vida del pokemon
    let newHpA = hpA - damageB;
    let newHpB = hpB - damageA;

    // Resultado del combate por consola
    console.log(`Turno del usuario: ${pokemonA.name} usa ${userMovement.name} y hace ${damageA} daño.`);
    console.log(`Turno de la máquina: ${pokemonB.name} usa ${opponentMovement.name} y hace ${damageB} daño.`);
    console.log(`Vida restante de ${pokemonA.name}: ${newHpA}`);
    console.log(`Vida restante de ${pokemonB.name}: ${newHpB}`);

    let winner = null;
    if (newHpA <= 0 && newHpB > 0) {
        winner = pokemonB.name;
    } else if (newHpB <= 0 && newHpA > 0) {
        winner = pokemonA.name;
    } else if (newHpA <= 0 && newHpB <= 0) {
        winner = "Empate";
    }

    if (winner) {
        if (winner === "Empate") {
            console.log("Los dos Pokémon han quedado fuera de combate.");
        } else {
            console.log(`${winner} ha ganado el combate`);
        }
    }
    
    return {
        hpA: newHpA,
        hpB: newHpB,
        userMovement,
        opponentMovement,
        damageA,
        damageB,
        winner
    };
}
