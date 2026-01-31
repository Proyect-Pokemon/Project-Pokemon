import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core'; // Permite hacer peticiones HTTP (GET, POST, etc.)
import { lastValueFrom, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Prueba {
  private readonly BASE_URL = 'https://localhost:7277/';
  private http = inject(HttpClient);

  async getData(): Promise<any> {
  const request: Observable<Object> = this.http.get(`${this.BASE_URL}WeatherForecast`);

  const data = await lastValueFrom(request); // Es como un request fetch pero usando RxJS

  return data;
  }
}