import { PokemonApi } from "../models/pokemon-api";
import { Move as Movement } from "../models/move";
import { CalculateDamage } from "./calculate-damage";

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

    // La potencia del movimiento es el daño que causa
    const damageA = userMovement.power ?? 0;
    const damageB = opponentMovement.power ?? 0;

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
        winner
    };
}
