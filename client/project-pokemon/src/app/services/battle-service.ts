import { inject, Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { BattleResponse } from '../models/pokemon-api';
import { ApiService } from './api';

// Este servicio se encarga de gestionar las llamadas a la API relacionadas con
// los Pokemon que participan en un combate
@Injectable({
  providedIn: 'root',
})
export class BattleService {
  private apiService = inject(ApiService);

  // Obtiene los datos del combate usando el servicio get de ApiService
  async getBattle(): Promise<BattleResponse | null> {
    return await this.apiService.get<BattleResponse>('battle');
  }
}