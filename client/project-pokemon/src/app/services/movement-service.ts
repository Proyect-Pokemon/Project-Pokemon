import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { Movement } from '../models/move';

@Injectable({
    providedIn: 'root'
})
export class MovementService {
    private readonly apiService = inject(ApiService);

    // PROVISIONAL: Este endpoint es temporal (GetAllMoves en MovementController).
    // Cambiar por el endpoint definitivo cuando esté disponible.
    async getAllMovements(): Promise<Movement[]> {
        return await this.apiService.get<Movement[]>('movement');
    }
}