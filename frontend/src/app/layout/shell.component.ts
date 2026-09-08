import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-shell">
      <header class="topbar">
        <div class="brand">
          <span class="brand-mark">SR</span>
          <div>
            <strong>Scheduled Reminders</strong>
            <p>Simulated delivery · JWT roles</p>
          </div>
        </div>
        <nav>
          <a routerLink="/reminders" routerLinkActive="active">Reminders</a>
          @if (auth.isAdmin()) {
            <a routerLink="/reminders/new" routerLinkActive="active">New reminder</a>
          }
        </nav>
        <div class="session">
          <span class="pill">{{ auth.role }}</span>
          <span class="username">{{ auth.username }}</span>
          <button type="button" class="ghost" (click)="auth.logout()">Sign out</button>
        </div>
      </header>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `
})
export class ShellComponent {
  constructor(readonly auth: AuthService) {}
}
