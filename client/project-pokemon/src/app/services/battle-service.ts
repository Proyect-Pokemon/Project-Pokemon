import { inject, Injectable } from '@angular/core';
import { BattleResponse } from '../models/pokemon-api';
import { BattleTurnRequest, BattleTurnResponse } from '../models/battle-turn';
import { ApiService } from './api';

// Este servicio se encarga de gestionar las llamadas a la API relacionadas con
// los Pokemon que participan en un combate
@Injectable({
  providedIn: 'root',
})
export class BattleService {
  private apiService = inject(ApiService);

  private buildStartBattlePayload(teamId: number): { teamId: number } {
    return { teamId };
  }

  // Obtiene los datos del combate usando el servicio get de ApiService
  async getBattle(): Promise<BattleResponse | null> {
    return await this.apiService.get<BattleResponse>('battle');
  }

  async startBattle(teamId: number): Promise<BattleResponse | null> {
    const payload = this.buildStartBattlePayload(teamId);

    try {
      return await this.apiService.post<BattleResponse>('battle/start', payload);
    } catch {
      try {
        return await this.apiService.post<BattleResponse>('battle', payload);
      } catch {
        return await this.apiService.get<BattleResponse>(`battle?teamId=${teamId}`);
      }
    }
  }

  // Envía el movimiento seleccionado al backend y obtiene el nuevo estado del turno.
  async playTurn(moveName: string): Promise<BattleTurnResponse | null> {
    const payload: BattleTurnRequest = { moveName };
    return await this.apiService.post<BattleTurnResponse>('battle/turn', payload);
  }
}