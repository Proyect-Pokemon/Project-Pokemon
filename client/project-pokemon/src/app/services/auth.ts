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
  AvatarPath?: string | null;
};

@Injectable({
  providedIn: 'root',
})
export class AuthService {

  private readonly jwtSignal = signal<string | null>(null);

  constructor(private api: ApiService) {
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
    const id = decoded?.id;

    if (!id) return null;

    const numeric = Number(id);
    return Number.isNaN(numeric) ? null : numeric;
  });

  readonly isAdmin = computed(() =>
    this.decodedPayload()?.role?.toLowerCase() === 'admin'
  );

  readonly nickname = computed(() => {
    const name = this.decodedPayload()?.unique_name?.trim();
    return name?.length ? name : 'Usuario';
  });

  readonly avatarPath = computed(() => {
    const path = this.decodedPayload()?.AvatarPath?.trim();
    return path?.length ? path : '/assets/avatar-default.png';
  });

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
    rememberMe = false
  ): Promise<boolean> {

    const response =
      await this.api.post<AuthResponse>('auth/login', authData);

    if (!response?.accessToken) {
      return false;
    }

    this.jwt = response.accessToken;

    if (rememberMe) {
      localStorage.setItem('jwt', response.accessToken);
    }

    return true;
  }

  async register(registerData: RegisterRequest): Promise<boolean> {
    await this.api.post('auth/register', registerData);
    return true;
  }

  async googleLogin(
    idToken: string,
    remember = true
  ): Promise<boolean> {

    if (!idToken?.trim()) {
      return false;
    }

    const response =
      await this.api.post<AuthResponse>(
        'auth/google',
        { idToken }
      );

    if (!response?.accessToken) {
      return false;
    }

    this.jwt = response.accessToken;

    if (remember) {
      localStorage.setItem('jwt', response.accessToken);
    }

    return true;
  }
}