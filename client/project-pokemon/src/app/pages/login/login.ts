import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthRequest } from '../../models/auth-request';
import { AuthService } from '../../services/auth';
import { SocketService } from '../../services/websocket-service';
import { environment } from '../../environments/environment';

declare const google: any;

@Component({
  selector: 'app-login',
  imports: [RouterLink, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit, OnDestroy {

  nickname = '';
  password = '';
  rememberMeChecked = false;

  errorMessage = signal('');

  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private readonly socketService = inject(SocketService);

  // Estática para que persista aunque el componente se destruya y recree
  private static googleInitialized = false;

  canSubmit(): boolean {
    return this.nickname.trim().length > 0 &&
      this.password.trim().length > 0;
  }

  async submit() {

    if (!this.canSubmit()) return;

    this.errorMessage.set('');

    const authData: AuthRequest = {
      nickname: this.nickname,
      password: this.password
    };

    try {

      const result = await this.authService.login(
        authData,
        this.rememberMeChecked
      );

      if (result !== true) {
        this.errorMessage.set('Usuario o contraseña incorrectos.');
        return;
      }

      const jwt = this.authService.jwt;
      if (jwt) this.socketService.connect(jwt);

      const redirectTo = this.resolveSafeRedirectTo(
        this.route.snapshot.queryParams['redirectTo']
      );

      await this.router.navigateByUrl(redirectTo);

    } catch (err: any) {

      if (err?.status === 0) {
        this.errorMessage.set('No se pudo establecer conexión con el servidor.');
        return;
      }

      const backendError =
        typeof err?.error === 'string'
          ? err.error
          : err?.error?.error ||
          err?.error?.message ||
          err?.message;

      this.errorMessage.set(
        backendError || 'Error de conexión con el servidor.'
      );
    }
  }

  ngOnInit() {

    document.body.classList.add('login-background');

    this.initGoogle();
  }

  private initGoogle(): void {
    if (Login.googleInitialized) return;
    if (typeof google === 'undefined') return;

    const clientId = this.getGoogleClientId();

    if (!clientId) return;

    const button = document.getElementById('googleButton');
    if (!button) return;

    try {
      google.accounts.id.initialize({
        client_id: clientId,
        callback: (response: any) => this.handleGoogle(response)
      });

      google.accounts.id.renderButton(button, {
        theme: 'outline',
        size: 'large'
      });

      Login.googleInitialized = true;
    } catch {
      // opcional: log
    }
  }

  private getGoogleClientId(): string {
    return environment.GOOGLE_CLIENT_ID;
  }

  async handleGoogle(response: any) {

    const idToken = response?.credential;

    if (!idToken) {
      this.errorMessage.set('Token de Google inválido');
      return;
    }

    try {

      const result = await this.authService.googleLogin(
        idToken,
        this.rememberMeChecked
      );

      if (result !== true) {
        this.errorMessage.set('Error al iniciar sesión con Google');
        return;
      }

      const jwt = this.authService.jwt;
      if (jwt) this.socketService.connect(jwt);

      const redirectTo = this.resolveSafeRedirectTo(
        this.route.snapshot.queryParams['redirectTo']
      );

      await this.router.navigateByUrl(redirectTo);

    } catch {
      this.errorMessage.set('Error al iniciar sesión con Google');
    }
  }

  ngOnDestroy() {
    document.body.classList.remove('login-background');
  }

  private resolveSafeRedirectTo(redirectToRaw: unknown): string {

    const redirectTo =
      typeof redirectToRaw === 'string' ? redirectToRaw : '';

    if (!redirectTo || !redirectTo.startsWith('/')) {
      return '/battle';
    }

    if (redirectTo.startsWith('/battle/fight')) {
      return '/battle-select?mode=online';
    }

    return redirectTo;
  }
}