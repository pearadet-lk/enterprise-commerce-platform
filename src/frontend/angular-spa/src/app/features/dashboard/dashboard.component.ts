import { Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DashboardStore } from './dashboard.store';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  readonly store = inject(DashboardStore);

  ngOnInit(): void {
    void this.store.loadSummary();
  }
}
