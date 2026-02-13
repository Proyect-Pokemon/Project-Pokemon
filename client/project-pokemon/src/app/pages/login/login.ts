import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink, Router, ActivatedRoute } from "@angular/router";
import { AuthService } from '../../services/auth';
import { FormsModule } from '@angular/forms';
import { AuthRequest } from '../../models/auth-request';

@Component({
  selector: 'app-login',
  imports: [RouterLink, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})

export class Login {

  // Variables del formulario de login
  nickname: string = '';
  password: string = '';
  rememberMeChecked: boolean = false;
  wrongCredentials = signal(false);

  // Inyectamos los servicios necesarios
  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  // Método del submit del formulario de login
  async submit() {
    const authData: AuthRequest = {
      nickname: this.nickname,
      password: this.password
    };

    const result = await this.authService.login(authData, this.rememberMeChecked);

    // Si el login es correcto, redirige al feed
    if (result.success) {
      // Obtenemos el parámetro redirectTo si existe, si no vamos a battle
      const redirectTo = this.route.snapshot.queryParams['redirectTo'] || '/battle';
      this.router.navigateByUrl(redirectTo);
    } else {
      this.wrongCredentials.set(true);
    }
  }
}
