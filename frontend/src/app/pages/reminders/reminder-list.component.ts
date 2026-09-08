import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription, interval, startWith, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ReminderApiService } from '../../core/reminder-api.service';
import { Reminder } from '../../models/models';

@Component({
  selector: 'app-reminder-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <div class="page-head">
      <div>
        <h1>Reminders</h1>
        <p>Due items are processed every 5 seconds. Delivery is simulated — nothing is emailed.</p>
      </div>
      @if (auth.isAdmin()) {
        <a class="button" routerLink="/reminders/new">Create reminder</a>
      }
    </div>

    @if (error) {
      <p class="error" role="alert">{{ error }}</p>
    } @else if (loading && reminders.length === 0) {
      <p class="muted">Loading reminders…</p>
    } @else if (reminders.length === 0) {
      <div class="empty">
        <h2>No reminders yet</h2>
        <p>
          @if (auth.isAdmin()) {
            Create one with a scheduled time in the past or near future to see the processor run.
          } @else {
            An admin has not created any reminders in this session.
          }
        </p>
      </div>
    } @else {
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Scheduled</th>
              <th>Frequency</th>
              <th>Runs left</th>
              <th>Status</th>
              <th>Active</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (reminder of reminders; track reminder.id) {
              <tr>
                <td>
                  <a [routerLink]="['/reminders', reminder.id]">{{ reminder.name }}</a>
                </td>
                <td>{{ reminder.scheduledAt | date: 'medium' }}</td>
                <td>{{ reminder.frequency }}</td>
                <td>{{ reminder.futureRunsCount }}</td>
                <td><span class="status" [attr.data-status]="reminder.status">{{ reminder.status }}</span></td>
                <td>{{ reminder.isActive ? 'Yes' : 'No' }}</td>
                <td class="actions">
                  @if (auth.isAdmin()) {
                    <a [routerLink]="['/reminders', reminder.id, 'edit']">Edit</a>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `
})
export class ReminderListComponent implements OnInit, OnDestroy {
  reminders: Reminder[] = [];
  loading = true;
  error = '';
  private sub?: Subscription;

  constructor(
    readonly auth: AuthService,
    private readonly api: ReminderApiService
  ) {}

  ngOnInit(): void {
    this.sub = interval(5000)
      .pipe(
        startWith(0),
        switchMap(() => this.api.list())
      )
      .subscribe({
        next: (reminders) => {
          this.reminders = reminders;
          this.loading = false;
          this.error = '';
        },
        error: () => {
          this.loading = false;
          this.error = 'Could not load reminders. Confirm the API is running.';
        }
      });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
