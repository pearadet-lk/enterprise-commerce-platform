import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { OrdersStore } from './orders.store';

@Component({
  selector: 'app-orders',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss'
})
export class OrdersComponent implements OnInit {
  readonly store = inject(OrdersStore);

  ngOnInit(): void {
    void this.store.loadOrders();
  }
}
