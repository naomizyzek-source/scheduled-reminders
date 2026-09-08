import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Reminder, ReminderExecution, ReminderRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReminderApiService {
  private readonly base = `${environment.apiUrl}/api/reminders`;

  constructor(private readonly http: HttpClient) {}

  list() {
    return this.http.get<Reminder[]>(this.base);
  }

  get(id: string) {
    return this.http.get<Reminder>(`${this.base}/${id}`);
  }

  create(body: ReminderRequest) {
    return this.http.post<Reminder>(this.base, body);
  }

  update(id: string, body: ReminderRequest) {
    return this.http.put<Reminder>(`${this.base}/${id}`, body);
  }

  executions(id: string) {
    return this.http.get<ReminderExecution[]>(`${this.base}/${id}/executions`);
  }
}
