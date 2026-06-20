import { computed, inject, Injectable, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { catchError, map, of } from 'rxjs';

export interface UserData {
  sub?: string;
  name?: string;
  role?: string | string[];
  email?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oidc = inject(OidcSecurityService);

  readonly isAuthenticated = signal(false);
  readonly userData = signal<UserData | null>(null);

  readonly roles = computed(() => {
    const role = this.userData()?.role;
    if (!role) {
      return [] as string[];
    }

    return Array.isArray(role) ? role : [role];
  });

  readonly isAdmin = computed(() => this.roles().includes('Admin'));

  constructor() {
    this.oidc.isAuthenticated$.subscribe(({ isAuthenticated }) => {
      this.isAuthenticated.set(isAuthenticated);
    });

    this.oidc.userData$.subscribe(({ userData }) => {
      this.userData.set((userData as UserData | null) ?? null);
    });
  }

  login(): void {
    this.oidc.authorize();
  }

  logout(): void {
    this.oidc.logoff().subscribe();
  }

  getAccessToken() {
    return this.oidc.getAccessToken();
  }

  checkAuth() {
    return this.oidc.checkAuth().pipe(
      map(({ isAuthenticated }) => isAuthenticated),
      catchError(() => of(false))
    );
  }
}
