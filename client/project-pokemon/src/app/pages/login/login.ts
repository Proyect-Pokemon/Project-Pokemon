import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink, Router, ActivatedRoute } from "@angular/router";
import { AuthRequest } from '../../models/auth-request';
import { AuthService } from '../../services/auth';
import { FormsModule } from '@angular/forms';
import { SocketService } from '../../services/websocket-service';

@Component({
  selector: 'app-login',
  imports: [RouterLink, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit, OnDestroy {

  // Variables del formulario de login
  nickname: string = '';
  password: string = '';
  rememberMeChecked: boolean = false;

  errorMessage = signal('');

  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private readonly socketService = inject(SocketService);

  canSubmit(): boolean {
    return this.nickname.trim().length > 0 && this.password.trim().length > 0;
  }

  async submit() {

    if (!this.canSubmit()) {
      return;
    }

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

      if (result === true) {
        const jwt = this.authService.jwt;
        if (jwt) this.socketService.connect(jwt);
        
        const redirectTo =
          this.route.snapshot.queryParams['redirectTo'] || '/battle';

        this.router.navigateByUrl(redirectTo);
        return;
      }

      this.errorMessage.set('Usuario o contraseña incorrectos.');

    } catch (err: any) {

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
  }

  ngOnDestroy() {
    document.body.classList.remove('login-background');
  }
}