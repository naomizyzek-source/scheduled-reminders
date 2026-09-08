import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="login-page">
      <section class="login-card">
        <p class="eyebrow">Scheduled Reminders</p>
        <h1>Sign in to manage deliveries</h1>
        <p class="lede">
          Backend JWT is the security boundary. Admins can create and edit reminders.
          Viewers can only inspect them and their simulated execution history.
        </p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>
            Username
            <input formControlName="username" autocomplete="username" />
          </label>
          <label>
            Password
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          @if (error) {
            <p class="error" role="alert">{{ error }}</p>
          }

          <button type="submit" [disabled]="form.invalid || loading">
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <div class="seed-help">
          <p><strong>Seeded accounts</strong></p>
          <p>admin / Admin123!</p>
          <p>viewer / Viewer123!</p>
        </div>
      </section>
    </div>
  `
})
export class LoginComponent {
  error = '';
  loading = false;
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading = true;
    this.error = '';
    const { username, password } = this.form.getRawValue();
    this.auth.login(username, password).subscribe({
      next: () => {
        void this.router.navigate(['/reminders']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.error = err.status === 401 ? 'Invalid username or password.' : 'Unable to sign in. Is the API running on port 5288?';
      }
    });
  }
}
