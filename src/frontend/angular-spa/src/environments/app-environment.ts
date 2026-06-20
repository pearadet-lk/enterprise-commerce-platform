import { InjectionToken } from '@angular/core';

export interface AppEnvironment {
  production: boolean;
  authority: string;
  redirectUrl: string;
  postLogoutRedirectUri: string;
  clientId: string;
  scope: string;
  apiGatewayUrl: string;
}

export const APP_ENVIRONMENT = new InjectionToken<AppEnvironment>('APP_ENVIRONMENT');

export const authConfig = {
  authority: 'http://localhost:5001',
  redirectUrl:
    typeof window !== 'undefined'
      ? window.location.origin + '/auth-callback'
      : 'http://localhost:4200/auth-callback',
  postLogoutRedirectUri:
    typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
  clientId: 'angular-spa',
  scope: 'openid profile roles catalog-api orders-api users-api',
  responseType: 'code' as const,
  silentRenew: true,
  useRefreshToken: true,
  renewTimeBeforeTokenExpiresInSeconds: 30
};
