import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth';
import { RegisterRequest } from '../../models/register-request';

export function strongPasswordValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';
  if (!value) return null;
  const errors: Record<string, boolean> = {};
  if (!/[A-Z]/.test(value))        errors['noUppercase'] = true;
  if (!/[0-9]/.test(value))        errors['noNumber'] = true;
  if (!/[^A-Za-z0-9]/.test(value)) errors['noSpecial'] = true;
  return Object.keys(errors).length ? errors : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, CommonModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css'],
})
export class Register implements OnInit, OnDestroy {

  registerForm: FormGroup;
  errorMessage = signal('');
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  constructor() {
    this.registerForm = this.fb.group({
      nickname: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6), strongPasswordValidator]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit() {
    document.body.classList.add('login-background');
  }

  ngOnDestroy() {
    document.body.classList.remove('login-background');
  }

  private passwordMatchValidator = (form: FormGroup) => {
    const password = form.get('password')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return password === confirm ? null : { passwordsMismatch: true };
  }

  getLabel(field: string): string {
    const labels: Record<string, string> = {
      nickname: 'Nickname',
      email: 'Correo electrónico',
      password: 'Contraseña',
      confirmPassword: 'Confirmar contraseña'
    };
    return labels[field] ?? field;
  }

  getAngularError(field: string): string {
    const control = this.registerForm.get(field);
    if (!control?.errors) return '';
    if (control.errors['required'])
      return `${this.getLabel(field)} es obligatorio.`;
    if (control.errors['minlength']) {
      const min = control.errors['minlength'].requiredLength;
      return `${this.getLabel(field)} debe tener mínimo ${min} caracteres.`;
    }
    if (control.errors['email'])
      return 'Correo electrónico inválido.';
    if (control.errors['noUppercase'])
      return 'La contraseña debe contener al menos una mayúscula.';
    if (control.errors['noNumber'])
      return 'La contraseña debe contener al menos un número.';
    if (control.errors['noSpecial'])
      return 'La contraseña debe contener al menos un carácter especial.';
    return '';
  }

  // Helpers para el indicador visual de requisitos
  get passwordValue(): string {
    return this.registerForm.get('password')?.value ?? '';
  }

  get reqLength(): boolean  { return this.passwordValue.length >= 6; }
  get reqUppercase(): boolean { return /[A-Z]/.test(this.passwordValue); }
  get reqNumber(): boolean    { return /[0-9]/.test(this.passwordValue); }
  get reqSpecial(): boolean   { return /[^A-Za-z0-9]/.test(this.passwordValue); }

  async submit() {
    this.errorMessage.set('');
    if (this.registerForm.invalid) {
      this.errorMessage.set('Revisa los campos obligatorios o errores.');
      return;
    }
    const formValue = this.registerForm.value;
    const registerData: RegisterRequest = {
      nickname: formValue.nickname,
      email: formValue.email,
      password: formValue.password
    };
    try {
      const success = await this.authService.register(registerData);
      if (success) {
        this.router.navigate(['/login']);
        return;
      }
      this.errorMessage.set('No se pudo registrar el usuario.');
    } catch (err: any) {
      const backendError =
        typeof err?.error === 'string'
          ? err.error
          : err?.error?.error || err?.error?.message || err?.message;
      this.errorMessage.set(backendError || 'Error de conexión con el servidor.');
    }
  }
}