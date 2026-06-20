import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { UsersStore } from './users.store';

@Component({
  selector: 'app-users',
  imports: [DatePipe],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  readonly store = inject(UsersStore);

  ngOnInit(): void {
    void this.store.loadUsers();
    void this.store.loadAuditLogs();
  }
}
