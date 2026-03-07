import { PokemonApi } from "../models/pokemon-api";
import { BattleMove as Movement } from "../models/move";

import { Injectable } from "@angular/core";
import { CalculateDamageService } from "./calculate-damage";
import { MoveSelection } from "./move-selection";
import { TurnOrder } from "./turn-order";

@Injectable({
    providedIn: 'root',
})
export class BattleSimulatorService {
    constructor(
        private moveSelector: MoveSelection,
        private turnOrder: TurnOrder,
        private damageService: CalculateDamageService
    ) {}

    simulateBattle(
        pokemonA: PokemonApi,
        pokemonB: PokemonApi,
        movementA: Movement,
        hpA: number,
        hpB: number
    ) {
        // Movimiento del usuario (pokemonA)
        const userMovement = movementA;
        // Movimiento del oponente (pokemonB)
        const opponentMovement = this.moveSelector.getRandomOpponentMove(pokemonB);

        // Delegar el cálculo del orden de turno
        const order = this.turnOrder.getTurnOrder(
            pokemonA,
            pokemonB,
            userMovement,
            opponentMovement,
            hpA,
            hpB
        );
        const { firstAttacker, secondAttacker, firstMovement, secondMovement, firstHp, secondHp, firstIsA } = order;

        // Calcular daño para ambos ataques
        const damageFirst = this.damageService.calculateDamage(
            this.calculateStab(firstMovement, firstAttacker),
            1,
            this.getRandomVariation(),
            this.getAttackStat(firstMovement, firstAttacker),
            firstMovement.power ?? 0,
            this.getDefenseStat(secondMovement, secondAttacker)
        );
        
        const damageSecond = this.damageService.calculateDamage(
            this.calculateStab(secondMovement, secondAttacker),
            1,
            this.getRandomVariation(),
            this.getAttackStat(secondMovement, secondAttacker),
            secondMovement.power ?? 0,
            this.getDefenseStat(firstMovement, firstAttacker)
        );

    // Restar el daño a la vida del defensor
    let firstTargetHp = secondHp - damageFirst;
    // Si el defensor sobrevive al primer ataque, el segundo atacante contraataca
    let secondTargetHp = firstHp;
    if (firstTargetHp > 0) {
        secondTargetHp = firstHp - damageSecond;
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
    }
    
    return {
        hpA: newHpA,
        hpB: newHpB,
        userMovement,
        opponentMovement,
        damageFirst,
        damageSecond,
        winner
    };
    }

    // Métodos auxiliares para reutilizar lógica
    private calculateStab(move: Movement, pokemon: PokemonApi): number {
        return move.type === pokemon.type1 || move.type === pokemon.type2 ? 1.5 : 1;
    }

    private getRandomVariation(): number {
        return Math.floor(Math.random() * (100 - 85 + 1)) + 85;
    }

    private getAttackStat(move: Movement, pokemon: PokemonApi): number {
        return move.moveClass === "Physical" ? pokemon.atk : pokemon.spa;
    }

    private getDefenseStat(move: Movement, pokemon: PokemonApi): number {
        return move.moveClass === "Physical" ? pokemon.def : pokemon.spd;
    }
}
