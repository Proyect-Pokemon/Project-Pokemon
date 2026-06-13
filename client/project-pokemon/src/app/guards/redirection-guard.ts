import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';

export const redirectionGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const isOnlineBattleRoute = state.url.startsWith('/battle/fight');

  // Verificar si existe el token JWT en el servicio o en localStorage
  if (authService.jwt) {
    return true;
  } else {
    // No preservamos rutas de combate online para impedir reentrada por URL vieja.
    if (isOnlineBattleRoute) {
      router.navigate(['/login']);
      return false;
    }

    // Si no hay token, se redirige a login con queryParam para volver después.
    router.navigate(['/login'], { queryParams: { redirectTo: state.url } });
    return false;
  }
};
