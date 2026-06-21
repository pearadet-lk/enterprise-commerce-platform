import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { APP_ENVIRONMENT } from '../../../environments/app-environment';
import { firstValueFrom } from 'rxjs';

export interface UserProfile {
  id: string;
  email: string;
  userName: string;
  fullName?: string;
  roles: string[];
  isActive: boolean;
}

export interface AuditLog {
  id: string;
  action: string;
  entityType: string;
  entityId?: string;
  userId: string;
  userName: string;
  timestamp: string;
  details?: string;
}

@Injectable({ providedIn: 'root' })
export class UsersStore {
  private readonly http = inject(HttpClient);
  private readonly env = inject(APP_ENVIRONMENT);

  readonly users = signal<UserProfile[]>([]);
  readonly auditLogs = signal<AuditLog[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadUsers(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const users = await firstValueFrom(
        this.http.get<UserProfile[]>(`${this.env.apiGatewayUrl}/api/users/users`)
      );
      this.users.set(users);
    } catch (err) {
      console.error('Failed to load users.', err);
      this.error.set('Unable to load users.');
    } finally {
      this.loading.set(false);
    }
  }

  async loadAuditLogs(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const logs = await firstValueFrom(
        this.http.get<AuditLog[]>(`${this.env.apiGatewayUrl}/api/auditlogs/auditlogs`)
      );
      this.auditLogs.set(logs);
    } catch (err) {
      console.error('Failed to load audit logs.', err);
      this.error.set('Unable to load audit logs.');
    } finally {
      this.loading.set(false);
    }
  }
}
