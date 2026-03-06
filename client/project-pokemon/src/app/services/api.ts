import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { Result } from '../models/result';

@Injectable({
  providedIn: 'root',
})
export class ApiService {

  private readonly BASE_URL = 'https://localhost:7277/api/';
  jwt: string | null = null;

  private http = inject(HttpClient);

  // Método para hacer peticiones GET a la API
  async get<T = void>(path: string): Promise<Result<T>> {
    try {
      const response = await lastValueFrom(
        this.http.get<T>(`${this.BASE_URL}${path}`, {
          headers: this.getHeaders(),
          observe: 'response'
        })
      );
      return Result.success(response.status, response.body as T);
    } catch (error: any) {
      return this.handleError(error);
    }
  }

  // Método para hacer peticiones POST a la API
  async post<T = void>(path: string, body: any): Promise<Result<T>> {
    try {
      const response = await lastValueFrom(
        this.http.post<T>(`${this.BASE_URL}${path}`, body, {
          headers: this.getHeaders(),
          observe: 'response'
        })
      );
      return Result.success(response.status, response.body as T);
    } catch (error: any) {
      return this.handleError(error);
    }
  }

  // Método para hacer peticiones PUT a la API
  async put<T = void>(path: string, body: any): Promise<Result<T>> {
    try {
      const response = await lastValueFrom(
        this.http.put<T>(`${this.BASE_URL}${path}`, body, {
          headers: this.getHeaders(),
          observe: 'response'
        })
      );
      return Result.success(response.status, response.body as T);
    } catch (error: any) {
      return this.handleError(error);
    }
  }

  // Método para hacer peticiones DELETE a la API
  async delete<T = void>(path: string): Promise<Result<T>> {
    try {
      const response = await lastValueFrom(
        this.http.delete<T>(`${this.BASE_URL}${path}`, {
          headers: this.getHeaders(),
          observe: 'response'
        })
      );
      return Result.success(response.status, response.body as T);
    } catch (error: any) {
      return this.handleError(error);
    }
  }

  // Método privado para manejar errores de forma centralizada
  private handleError<T = void>(error: any): Result<T> {
    const status = error instanceof HttpErrorResponse ? error.status : 500;
    const message = error instanceof HttpErrorResponse
      ? (error.error?.message || error.message || error.statusText)
      : error.message || 'Unknown error';
    return Result.error(status, message);
  }

  // Método para obtener los headers de las peticiones incluyendo el JWT
  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    if (this.jwt) {
      headers = headers.set('Authorization', `Bearer ${this.jwt}`);
    }

    return headers;
  }
}
