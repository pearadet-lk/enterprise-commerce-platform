import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { catchError, of, take } from 'rxjs';

@Component({
  selector: 'app-auth-callback',
  template: '<p>Completing sign-in...</p>'
})
export class AuthCallbackComponent implements OnInit {
  private readonly oidc = inject(OidcSecurityService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.oidc
      .checkAuth(window.location.href)
      .pipe(
        take(1),
        catchError(() => of({ isAuthenticated: false }))
      )
      .subscribe(({ isAuthenticated }) => {
        void this.router.navigateByUrl(isAuthenticated ? '/dashboard' : '/login');
      });
  }
}
