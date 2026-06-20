import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const oidc = inject(OidcSecurityService);

  if (req.url.includes('/connect/') || req.url.includes('localhost:5001')) {
    return next(req);
  }

  return oidc.getAccessToken().pipe(
    switchMap((token) => {
      if (!token) {
        return next(req);
      }

      return next(
        req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        })
      );
    })
  );
};
