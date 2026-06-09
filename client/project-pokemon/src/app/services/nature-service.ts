import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { Nature } from '../models/nature';

@Injectable({
  providedIn: 'root',
})
export class NatureService {
  private readonly api = inject(ApiService);

  async getAllNatures(): Promise<Nature[]> {
    return this.api.get<Nature[]>('nature');
  }
}