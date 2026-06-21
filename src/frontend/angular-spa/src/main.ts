import { bootstrapApplication } from '@angular/platform-browser';
import { createAppConfig } from './app/app.config';
import { App } from './app/app';
import { AppEnvironment } from './environments/app-environment';
import { environment } from './environments/environment';

async function loadRuntimeEnvironment(): Promise<AppEnvironment> {
  try {
    const response = await fetch('/app-config.json', { cache: 'no-store' });
    if (!response.ok) {
      return environment;
    }

    const runtime = (await response.json()) as Partial<AppEnvironment>;
    return { ...environment, ...runtime };
  } catch (err) {
    console.error('Failed to load /app-config.json; using built-in environment.', err);
    return environment;
  }
}

loadRuntimeEnvironment()
  .then((runtimeEnvironment) => bootstrapApplication(App, createAppConfig(runtimeEnvironment)))
  .catch((err) => console.error(err));
