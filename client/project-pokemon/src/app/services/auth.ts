import { computed, Injectable, signal } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { ApiService } from './api';
import { AuthRequest } from '../models/auth-request';
import { AuthResponse } from '../models/auth-response';
import { RegisterRequest } from '../models/register-request';

type JwtPayload = {
  id?: string | number;
  role?: string;
  unique_name?: string;
};

@Injectable({
  providedIn: 'root',
})
export class AuthService {

  private readonly jwtSignal = signal<string | null>(null);

  constructor(private api: ApiService) {

    // Restaurar sesión
    const jwt = localStorage.getItem('jwt');
    if (jwt) {
      this.setJwt(jwt);
    }

  }

  private readonly decodedPayload = computed<JwtPayload | null>(() => {
    const token = this.jwtSignal();
    if (!token) return null;

    try {
      return jwtDecode<JwtPayload>(token);
    } catch {
      return null;
    }
  });

  readonly isAuthenticated = computed(() => !!this.jwtSignal());

  readonly currentUserId = computed(() => {
    const decoded = this.decodedPayload();
    if (!decoded?.id) return null;

    const id = Number(decoded.id);
    return Number.isNaN(id) ? null : id;
  });

  readonly isAdmin = computed(() =>
    this.decodedPayload()?.role?.toLowerCase() === 'admin'
  );

  get jwt(): string | null {
    return this.jwtSignal();
  }

  set jwt(value: string | null) {
    this.jwtSignal.set(value);
    this.api.jwt = value;
  }

  setJwt(jwt: string): void {
    this.jwt = jwt;
  }

  async login(
    authData: AuthRequest,
    rememberMe: boolean = false
  ): Promise<boolean> {

    const response = await this.api.post<AuthResponse>('auth/login', authData);

    if (response?.accessToken) {

      this.jwt = response.accessToken;

      if (rememberMe) {
        localStorage.setItem('jwt', response.accessToken);
      }

      return true;
    }

    return false;
  }

  async register(registerData: RegisterRequest): Promise<boolean> {
    await this.api.post('auth/register', registerData);
    return true;
  }
}