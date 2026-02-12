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

    // Determinar el orden de ataque según la velocidad (spe)
    let firstAttacker, secondAttacker;
    let firstMovement, secondMovement;
    let firstHp, secondHp;
    let firstIsA = true;
    if (pokemonA.spe > pokemonB.spe) {
        firstAttacker = pokemonA;
        firstMovement = userMovement;
        firstHp = hpA;
        secondAttacker = pokemonB;
        secondMovement = opponentMovement;
        secondHp = hpB;
        firstIsA = true;
    } else if (pokemonB.spe > pokemonA.spe) {
        firstAttacker = pokemonB;
        firstMovement = opponentMovement;
        firstHp = hpB;
        secondAttacker = pokemonA;
        secondMovement = userMovement;
        secondHp = hpA;
        firstIsA = false;
    } else {
        // Si la velocidad es igual, elegir al azar
        if (Math.random() < 0.5) {
            firstAttacker = pokemonA;
            firstMovement = userMovement;
            firstHp = hpA;
            secondAttacker = pokemonB;
            secondMovement = opponentMovement;
            secondHp = hpB;
            firstIsA = true;
        } else {
            firstAttacker = pokemonB;
            firstMovement = opponentMovement;
            firstHp = hpB;
            secondAttacker = pokemonA;
            secondMovement = userMovement;
            secondHp = hpA;
            firstIsA = false;
        }
    }

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
    let damageFirst, damageSecond;
    if (firstIsA) {
        damageFirst = damageService.calculateDamage(stabA, effectiveA, variationA, attackA, powerA ?? 0, defenseA);
        damageSecond = damageService.calculateDamage(stabB, effectiveB, variationB, attackB, powerB ?? 0, defenseB);
    } else {
        damageFirst = damageService.calculateDamage(stabB, effectiveB, variationB, attackB, powerB ?? 0, defenseB);
        damageSecond = damageService.calculateDamage(stabA, effectiveA, variationA, attackA, powerA ?? 0, defenseA);
    }

    // Restar el daño a la vida del defensor
    let firstTargetHp = secondHp - damageFirst;
    let secondTargetHp = firstHp - damageSecond;

    // Mostrar el orden de ataque y resultados
    console.log(`Turno 1: ${firstAttacker.name} usa ${firstMovement.name} y hace ${damageFirst} daño.`);
    console.log(`Vida restante de ${secondAttacker.name}: ${firstTargetHp}`);
    // Si el primer defensor sobrevive, el segundo ataca
    if (firstTargetHp > 0) {
        console.log(`Turno 2: ${secondAttacker.name} usa ${secondMovement.name} y hace ${damageSecond} daño.`);
        console.log(`Vida restante de ${firstAttacker.name}: ${secondTargetHp}`);
    }

    // Asignar los nuevos HP a A y B según el orden
    let newHpA, newHpB;
    if (firstIsA) {
        newHpA = secondTargetHp;
        newHpB = firstTargetHp;
    } else {
        newHpA = firstTargetHp;
        newHpB = secondTargetHp;
    }

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
        damageFirst, // Daño infligido por el primer atacante
        damageSecond, // Daño infligido por el segundo atacante (si ataca)
        winner
    };
}
