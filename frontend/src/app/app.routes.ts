import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './core/auth.guards';
import { LoginComponent } from './pages/login/login.component';
import { ShellComponent } from './layout/shell.component';
import { ReminderListComponent } from './pages/reminders/reminder-list.component';
import { ReminderFormComponent } from './pages/reminders/reminder-form.component';
import { ReminderDetailComponent } from './pages/reminders/reminder-detail.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'reminders' },
      { path: 'reminders', component: ReminderListComponent },
      { path: 'reminders/new', component: ReminderFormComponent, canActivate: [adminGuard] },
      { path: 'reminders/:id/edit', component: ReminderFormComponent, canActivate: [adminGuard] },
      { path: 'reminders/:id', component: ReminderDetailComponent }
    ]
  },
  { path: '**', redirectTo: 'reminders' }
];
