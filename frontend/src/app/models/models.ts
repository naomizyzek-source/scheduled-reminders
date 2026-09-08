export type Frequency = 'Once' | 'Daily' | 'Weekly' | 'Monthly';
export type ReminderStatus = 'Pending' | 'Running' | 'Success' | 'Failed';

export interface LoginResponse {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

export interface Reminder {
  id: string;
  name: string;
  message: string;
  scheduledAt: string;
  frequency: Frequency;
  isActive: boolean;
  futureRunsCount: number;
  status: ReminderStatus;
  createdAt: string;
  updatedAt: string;
}

export interface ReminderRequest {
  name: string;
  message: string;
  scheduledAt: string;
  frequency: Frequency;
  isActive: boolean;
  futureRunsCount: number;
}

export interface ReminderExecution {
  id: string;
  reminderId: string;
  executedAt: string;
  status: ReminderStatus;
  detail: string;
}
