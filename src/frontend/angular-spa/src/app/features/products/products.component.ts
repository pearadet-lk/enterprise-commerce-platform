import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { ProductsStore } from './products.store';

@Component({
  selector: 'app-products',
  imports: [CurrencyPipe],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent implements OnInit {
  readonly store = inject(ProductsStore);

  ngOnInit(): void {
    void this.store.loadProducts();
  }
}
