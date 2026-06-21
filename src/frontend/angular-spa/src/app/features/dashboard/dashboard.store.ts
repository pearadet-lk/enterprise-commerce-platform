import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { APP_ENVIRONMENT } from '../../../environments/app-environment';
import { firstValueFrom } from 'rxjs';

export interface DashboardSummary {
  users: number;
  auditLogs: number;
  generatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class DashboardStore {
  private readonly http = inject(HttpClient);
  private readonly env = inject(APP_ENVIRONMENT);

  readonly summary = signal<DashboardSummary | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadSummary(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const data = await firstValueFrom(
        this.http.get<{ users: number; auditLogs: number; generatedAt: string }>(
          `${this.env.apiGatewayUrl}/api/dashboard/summary`
        )
      );

      this.summary.set({
        users: data.users,
        auditLogs: data.auditLogs,
        generatedAt: data.generatedAt
      });
    } catch (err) {
      console.error('Failed to load dashboard summary.', err);
      this.error.set('Unable to load dashboard summary.');
    } finally {
      this.loading.set(false);
    }
  }
}
