import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CalculateDamage {
  calculateDamage(stab: number, effective: number, variation: number, attackersLevel: number, attack: number, power: number, defense: number) {
    // Fórmula para calcular el daño

    // stab: 1.5 si el ataque es del mismo tipo que el Pokémon atacante, de lo contrario 1
    // effective: 2 si el ataque es súper efectivo, 0.5 si es poco efectivo, 1 si es neutral y 0 si es inmune
    // variation: un número aleatorio entre 85 y 100
    // attackersLevel: nivel del Pokémon atacante
    // attack: estadística de ataque del Pokémon atacante (especial o física, dependiendo del movimiento)
    // power: potencia del movimiento
    // defense: estadística de defensa del Pokémon defensor (especial o física, dependiendo del movimiento)

    // Devuelve el daño que ha hecho al Pokemon defensor
    return Math.floor(0.01 * stab * effective * variation * ((((0.2 * attackersLevel + 1) * attack * power) / (25 * defense)) + 2));
  } 
}
