import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse } from '../models/models';

const STORAGE_KEY = 'scheduled-reminders.auth';

interface StoredSession {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session$ = new BehaviorSubject<StoredSession | null>(this.readSession());

  readonly user$ = this.session$.asObservable();

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  get token(): string | null {
    return this.session$.value?.token ?? null;
  }

  get username(): string | null {
    return this.session$.value?.username ?? null;
  }

  get role(): string | null {
    return this.session$.value?.role ?? null;
  }

  isLoggedIn(): boolean {
    const session = this.session$.value;
    if (!session) {
      return false;
    }
    return new Date(session.expiresAt).getTime() > Date.now();
  }

  isAdmin(): boolean {
    return this.role === 'Admin';
  }

  login(username: string, password: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, { username, password })
      .pipe(
        tap((response) => {
          const session: StoredSession = {
            token: response.token,
            username: response.username,
            role: response.role,
            expiresAt: response.expiresAt
          };
          localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
          this.session$.next(session);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session$.next(null);
    void this.router.navigate(['/login']);
  }

  private readSession(): StoredSession | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      const parsed = JSON.parse(raw) as StoredSession;
      if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
