import { AppEnvironment } from './app-environment';

export const environment: AppEnvironment = {
  production: false,
  authority: 'http://localhost:5001',
  redirectUrl: 'http://localhost:4200/auth-callback',
  postLogoutRedirectUri: 'http://localhost:4200',
  clientId: 'angular-spa',
  scope: 'openid profile roles catalog-api orders-api users-api',
  apiGatewayUrl: 'http://localhost:7000'
};
