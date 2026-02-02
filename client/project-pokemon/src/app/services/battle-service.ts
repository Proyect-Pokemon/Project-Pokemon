import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { lastValueFrom, Observable } from 'rxjs';
import { BattleResponse } from '../models/pokemon-api';

@Injectable({
  providedIn: 'root',
})
export class BattleService {
  private readonly BASE_URL = 'https://localhost:7277/api/battle';
  private http = inject(HttpClient);

  async getBattle(): Promise<BattleResponse> {
    return await lastValueFrom(
      this.http.get<BattleResponse>(this.BASE_URL)
    );
  }
}