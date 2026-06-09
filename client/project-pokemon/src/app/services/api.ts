import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ApiService {

  private readonly BASE_URL = 'https://localhost:7277/api/';
  private readonly http = inject(HttpClient);

  jwt: string | null = null;

  async get<T>(path: string): Promise<T> {
    return await lastValueFrom(
      this.http.get<T>(`${this.BASE_URL}${path}`)
    );
  }

  async post<T>(path: string, body: any): Promise<T> {
    return await lastValueFrom(
      this.http.post<T>(`${this.BASE_URL}${path}`, body)
    );
  }

  async put<T>(path: string, body: any): Promise<T> {
    return await lastValueFrom(
      this.http.put<T>(`${this.BASE_URL}${path}`, body)
    );
  }

  async delete<T>(path: string): Promise<T> {
    return await lastValueFrom(
      this.http.delete<T>(`${this.BASE_URL}${path}`)
    );
  }
}