import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { APP_ENVIRONMENT } from '../../../environments/app-environment';
import { firstValueFrom } from 'rxjs';

export interface OrderLine {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerName: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  lines: OrderLine[];
}

@Injectable({ providedIn: 'root' })
export class OrdersStore {
  private readonly http = inject(HttpClient);
  private readonly env = inject(APP_ENVIRONMENT);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadOrders(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const orders = await firstValueFrom(
        this.http.get<Order[]>(`${this.env.apiGatewayUrl}/api/orders/orders`)
      );
      this.orders.set(orders);
    } catch (err) {
      console.error('Failed to load orders.', err);
      this.error.set('Unable to load orders.');
    } finally {
      this.loading.set(false);
    }
  }
}
