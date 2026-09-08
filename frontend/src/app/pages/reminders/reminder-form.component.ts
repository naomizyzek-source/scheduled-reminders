import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReminderApiService } from '../../core/reminder-api.service';
import { Frequency, ReminderRequest } from '../../models/models';

@Component({
  selector: 'app-reminder-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <p class="crumb"><a routerLink="/reminders">Reminders</a> / {{ isEdit ? 'Edit' : 'Create' }}</p>
        <h1>{{ isEdit ? 'Edit reminder' : 'New reminder' }}</h1>
      </div>
    </div>

    @if (loadError) {
      <p class="error" role="alert">{{ loadError }}</p>
    } @else {
      <form class="card form-grid" [formGroup]="form" (ngSubmit)="save()">
        <label>
          Name
          <input formControlName="name" maxlength="200" />
        </label>
        <label class="wide">
          Message
          <textarea formControlName="message" rows="4" maxlength="2000"></textarea>
        </label>
        <label>
          Scheduled at
          <input type="datetime-local" formControlName="scheduledAt" />
        </label>
        <label>
          Frequency
          <select formControlName="frequency">
            <option value="Once">Once</option>
            <option value="Daily">Daily</option>
            <option value="Weekly">Weekly</option>
            <option value="Monthly">Monthly</option>
          </select>
        </label>
        <label>
          Future runs count
          <input type="number" min="0" formControlName="futureRunsCount" />
          <span class="hint">User-supplied remaining executions — not calculated from frequency.</span>
        </label>
        <label class="checkbox">
          <input type="checkbox" formControlName="isActive" />
          Active
        </label>

        @if (error) {
          <p class="error wide" role="alert">{{ error }}</p>
        }

        <div class="form-actions wide">
          <button type="submit" [disabled]="form.invalid || saving">
            {{ saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create reminder' }}
          </button>
          <a class="ghost-link" routerLink="/reminders">Cancel</a>
        </div>
      </form>
    }
  `
})
export class ReminderFormComponent implements OnInit {
  isEdit = false;
  reminderId = '';
  saving = false;
  error = '';
  loadError = '';
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ReminderApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    message: ['', [Validators.required, Validators.maxLength(2000)]],
    scheduledAt: ['', Validators.required],
    frequency: this.fb.nonNullable.control<Frequency>('Once', Validators.required),
    futureRunsCount: [1, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.reminderId = this.route.snapshot.paramMap.get('id') ?? '';
    this.isEdit = !!this.reminderId && this.route.snapshot.routeConfig?.path?.includes('edit') === true;

    if (!this.isEdit) {
      const soon = new Date(Date.now() + 60_000);
      this.form.patchValue({ scheduledAt: toLocalInput(soon) });
      return;
    }

    this.api.get(this.reminderId).subscribe({
      next: (reminder) => {
        this.form.patchValue({
          name: reminder.name,
          message: reminder.message,
          scheduledAt: toLocalInput(new Date(reminder.scheduledAt)),
          frequency: reminder.frequency,
          futureRunsCount: reminder.futureRunsCount,
          isActive: reminder.isActive
        });
      },
      error: () => {
        this.loadError = 'Reminder not found.';
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const body: ReminderRequest = {
      name: value.name.trim(),
      message: value.message.trim(),
      scheduledAt: new Date(value.scheduledAt).toISOString(),
      frequency: value.frequency,
      futureRunsCount: Number(value.futureRunsCount),
      isActive: value.isActive
    };

    this.saving = true;
    this.error = '';
    const request$ = this.isEdit
      ? this.api.update(this.reminderId, body)
      : this.api.create(body);

    request$.subscribe({
      next: (reminder) => {
        void this.router.navigate(['/reminders', reminder.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving = false;
        this.error = formatApiError(err);
      }
    });
  }
}

function toLocalInput(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function formatApiError(err: HttpErrorResponse): string {
  if (err.status === 403) {
    return 'You are not allowed to change reminders.';
  }
  if (err.status === 400 && err.error?.errors) {
    const parts = Object.entries(err.error.errors as Record<string, string[]>)
      .flatMap(([field, messages]) => messages.map((m) => `${field}: ${m}`));
    return parts.join(' ');
  }
  if (err.status === 0) {
    return 'Could not reach the API. Start the backend on http://localhost:5288.';
  }
  return 'Save failed. Check the form and try again.';
}
