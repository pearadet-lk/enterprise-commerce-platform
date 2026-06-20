import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor, provideAuth } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { APP_ENVIRONMENT, authConfig } from '../environments/app-environment';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor()])),
    { provide: APP_ENVIRONMENT, useValue: environment },
    provideAuth({
      config: authConfig
    })
  ]
};
