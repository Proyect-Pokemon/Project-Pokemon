import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { lastValueFrom, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class BattleService {
  private readonly BASE_URL = 'https://localhost:7277/api/battle';
  private http = inject(HttpClient);

  async getBattle(): Promise<any> {
    const request: Observable<Object> = this.http.get(this.BASE_URL);
    const data = await lastValueFrom(request);
    return data;
  }
}


