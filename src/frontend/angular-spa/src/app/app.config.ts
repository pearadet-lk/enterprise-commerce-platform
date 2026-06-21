import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor, provideAuth } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { APP_ENVIRONMENT, AppEnvironment } from '../environments/app-environment';
import { environment } from '../environments/environment';

export function createAppConfig(runtimeEnvironment: AppEnvironment): ApplicationConfig {
  const authConfig = {
    authority: runtimeEnvironment.authority,
    redirectUrl: runtimeEnvironment.redirectUrl,
    postLogoutRedirectUri: runtimeEnvironment.postLogoutRedirectUri,
    clientId: runtimeEnvironment.clientId,
    scope: runtimeEnvironment.scope,
    responseType: 'code' as const,
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    secureRoutes: [`${runtimeEnvironment.apiGatewayUrl}/api/`]
  };

  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes),
      provideHttpClient(withInterceptors([authInterceptor()])),
      { provide: APP_ENVIRONMENT, useValue: runtimeEnvironment },
      provideAuth({ config: authConfig })
    ]
  };
}

export const appConfig = createAppConfig(environment);
