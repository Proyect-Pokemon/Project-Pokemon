import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';

/**
 * Servicio base genérico para operaciones CRUD contra la API.
 * Proporciona métodos comunes para GET y POST que reutilizan la lógica de manejo de errores.
 */
@Injectable({
  providedIn: 'root',
})
export class BaseApiService {
  protected apiService = inject(ApiService);

  private hasSuccessFlag(value: unknown): value is { success: boolean } {
    return typeof value === 'object' && value !== null && 'success' in value && typeof (value as { success?: unknown }).success === 'boolean';
  }

  /**
   * Obtiene una lista de items de la API
   * @param endpoint El endpoint de la API (ej: 'pokemon', 'team')
   * @returns Array de items o array vacío si hay error
   */
  async getList<T>(endpoint: string): Promise<T[]> {
    return await this.apiService.get<T[]>(endpoint);
  }

  /**
   * Crea un nuevo item en la API
   * @param endpoint El endpoint de la API
   * @param data Los datos a enviar
   * @returns true si la operación fue exitosa, false en caso contrario
   */
  async create<T>(endpoint: string, data: T): Promise<boolean> {
    try {
      await this.apiService.post<T>(endpoint, data);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Actualiza un item en la API
   * @param endpoint El endpoint de la API
   * @param data Los datos a enviar
   * @returns true si la operación fue exitosa, false en caso contrario
   */
  async update<T>(endpoint: string, data: T): Promise<boolean> {
    try {
      const result = await this.apiService.put<unknown>(endpoint, data);
      return this.hasSuccessFlag(result) ? result.success : true;
    } catch {
      return false;
    }
  }
}
