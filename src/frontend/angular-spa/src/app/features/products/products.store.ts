import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { APP_ENVIRONMENT } from '../../../environments/app-environment';
import { firstValueFrom } from 'rxjs';

export interface Product {
  id: string;
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  stockQuantity: number;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductsStore {
  private readonly http = inject(HttpClient);
  private readonly env = inject(APP_ENVIRONMENT);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadProducts(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const products = await firstValueFrom(
        this.http.get<Product[]>(`${this.env.apiGatewayUrl}/api/catalog/products`)
      );
      this.products.set(products);
    } catch {
      this.error.set('Unable to load products.');
    } finally {
      this.loading.set(false);
    }
  }
}
