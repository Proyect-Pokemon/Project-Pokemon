import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth';
import { SocketService } from './websocket-service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const authService = inject(AuthService);
  const router = inject(Router);
  const jwt = authService.jwt;
  const socketService = inject(SocketService);

  if (!jwt) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${jwt}`
    }
  });

  return next(authReq).pipe(
    catchError((err) => {
      if (err.status === 401) {
        authService.jwt = null;
        localStorage.removeItem('jwt');
        socketService.disconnect();
        void router.navigate(['/login']);
      }
      return throwError(() => err);
    })
  );
};