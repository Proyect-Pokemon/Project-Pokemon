import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthRequest } from '../../models/auth-request';
import { AuthService } from '../../services/auth';
import { SocketService } from '../../services/websocket-service';

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
  private socketService = inject(SocketService);

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

      if (!result) {
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

    if (typeof google === 'undefined') return;

    google.accounts.id.initialize({
      client_id: 'TU_CLIENT_ID.apps.googleusercontent.com',
      callback: (response: any) => this.handleGoogle(response)
    });

    const button = document.getElementById('googleButton');

    if (button) {
      google.accounts.id.renderButton(button, {
        theme: 'outline',
        size: 'large'
      });
    }
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

      if (!result) {
        this.errorMessage.set('Error al iniciar sesión con Google');
        return;
      }

      const jwt = this.authService.jwt;
      if (jwt) this.socketService.connect(jwt);

      await this.router.navigateByUrl('/battle');

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