import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription, forkJoin, interval, startWith, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ReminderApiService } from '../../core/reminder-api.service';
import { Reminder, ReminderExecution } from '../../models/models';

@Component({
  selector: 'app-reminder-detail',
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <p class="crumb"><a routerLink="/reminders">Reminders</a> / Detail</p>
        <h1>{{ reminder?.name || 'Reminder' }}</h1>
      </div>
      @if (auth.isAdmin() && reminder) {
        <a class="button" [routerLink]="['/reminders', reminder.id, 'edit']">Edit</a>
      }
    </div>

    @if (error) {
      <p class="error" role="alert">{{ error }}</p>
    } @else if (!reminder) {
      <p class="muted">Loading reminder…</p>
    } @else {
      <section class="card details">
        <dl>
          <div><dt>Message</dt><dd>{{ reminder.message }}</dd></div>
          <div><dt>Scheduled at</dt><dd>{{ reminder.scheduledAt | date: 'medium' }}</dd></div>
          <div><dt>Frequency</dt><dd>{{ reminder.frequency }}</dd></div>
          <div><dt>Future runs</dt><dd>{{ reminder.futureRunsCount }}</dd></div>
          <div><dt>Status</dt><dd><span class="status" [attr.data-status]="reminder.status">{{ reminder.status }}</span></dd></div>
          <div><dt>Active</dt><dd>{{ reminder.isActive ? 'Yes' : 'No' }}</dd></div>
        </dl>
      </section>

      <section class="history">
        <h2>Execution history</h2>
        <p class="muted">Each attempt is recorded, including Failed sends. Recurring reminders consume one FutureRunsCount per attempt and reschedule when runs remain.</p>

        @if (executions.length === 0) {
          <div class="empty compact">
            <h2>No executions yet</h2>
            <p>When ScheduledAt is due and the reminder is active with remaining runs, the hosted service records a simulated send here.</p>
          </div>
        } @else {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Started</th>
                  <th>Completed</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                @for (item of executions; track item.id) {
                  <tr>
                    <td>{{ item.startedAt | date: 'medium' }}</td>
                    <td>{{ item.completedAt ? (item.completedAt | date: 'medium') : '—' }}</td>
                    <td><span class="status" [attr.data-status]="item.status">{{ item.status }}</span></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    }
  `
})
export class ReminderDetailComponent implements OnInit, OnDestroy {
  reminder: Reminder | null = null;
  executions: ReminderExecution[] = [];
  error = '';
  private sub?: Subscription;

  constructor(
    readonly auth: AuthService,
    private readonly api: ReminderApiService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Missing reminder id.';
      return;
    }

    this.sub = interval(5000)
      .pipe(
        startWith(0),
        switchMap(() => forkJoin({
          reminder: this.api.get(id),
          executions: this.api.executions(id)
        }))
      )
      .subscribe({
        next: ({ reminder, executions }) => {
          this.reminder = reminder;
          this.executions = executions;
          this.error = '';
        },
        error: () => {
          this.error = 'Reminder not found.';
          this.reminder = null;
        }
      });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
