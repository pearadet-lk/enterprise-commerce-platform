import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.checkAuth().pipe(
    map((isAuthenticated) => isAuthenticated || router.createUrlTree(['/login'])),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.checkAuth().pipe(
    map((isAuthenticated) => {
      if (!isAuthenticated) {
        authService.login();
        return false;
      }

      if (authService.isAdmin()) {
        return true;
      }

      return router.createUrlTree(['/dashboard']);
    })
  );
};
