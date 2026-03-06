import { Component } from '@angular/core';
import { RegisterRequest } from '../../models/register-request';

@Component({
  selector: 'app-register',
  imports: [],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  //Variables del formulario
  nickname: string = '';
  email: string = '';
  password: string = '';

  async register() {
    const registerData: RegisterRequest = {
      email: this.email,
      nickname: this.nickname,
      password: this.password

    }
  }
}
