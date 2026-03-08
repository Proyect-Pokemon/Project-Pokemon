import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { Movement } from '../models/move';

@Injectable({
    providedIn: 'root'
})
export class MovementService {
    private readonly apiService = inject(ApiService);

    async getAllMovements(): Promise<Movement[]> {
        return await this.apiService.get<Movement[]>('movement');
    }
}