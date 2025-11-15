import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DashboardOverview {
  userMetrics: UserMetrics;
  contentMetrics: ContentMetrics;
  engagementMetrics: EngagementMetrics;
  securityMetrics: SecurityMetrics;
}

export interface UserMetrics {
  totalUsers: number;
  activeUsers: number;
  newUsersLast7Days: number;
  newUsersLast30Days: number;
  lockedAccounts: number;
  verifiedEmails: number;
  unverifiedEmails: number;
  usersWithFailedAttempts: number;
  usersByRole: { [key: string]: number };
}

export interface ContentMetrics {
  totalPosts: number;
  postsLast7Days: number;
  postsLast30Days: number;
  totalComments: number;
  totalLikes: number;
  averagePostsPerUser: number;
}

export interface EngagementMetrics {
  totalFollows: number;
  averageFollowersPerUser: number;
  engagementRate: number;
}

export interface SecurityMetrics {
  activeSessions: number;
  totalFailedAttempts: number;
  failedAttemptsLast24Hours: number;
  failedAttemptsLast7Days: number;
  lockedAccountsCount: number;
  oAuthLogins: number;
  passwordLogins: number;
}

export interface TimeSeriesData {
  data: TimeSeriesPoint[];
}

export interface TimeSeriesPoint {
  date: string;
  value: number;
  label?: string;
}

export interface ActiveSession {
  sessionId: string;
  userId: string;
  userName: string;
  userEmail: string;
  deviceInfo?: string;
  ipAddress?: string;
  createdAt: string;
  expiresAt: string;
  duration: string;
  isExpired: boolean;
}

export interface FailedLoginAttempt {
  userId: string;
  userName: string;
  userEmail: string;
  failedAttempts: number;
  isLocked: boolean;
  lockoutEnd?: string;
  lastLoginAttempt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) { }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  getOverview(): Observable<DashboardOverview> {
    return this.http.get<DashboardOverview>(`${this.apiUrl}/overview`, { headers: this.getHeaders() });
  }

  getUserGrowth(days: number = 30): Observable<TimeSeriesData> {
    return this.http.get<TimeSeriesData>(`${this.apiUrl}/users/growth?days=${days}`, { headers: this.getHeaders() });
  }

  getPostActivity(days: number = 30): Observable<TimeSeriesData> {
    return this.http.get<TimeSeriesData>(`${this.apiUrl}/posts/activity?days=${days}`, { headers: this.getHeaders() });
  }

  getLoginActivity(days: number = 30): Observable<TimeSeriesData> {
    return this.http.get<TimeSeriesData>(`${this.apiUrl}/login/activity?days=${days}`, { headers: this.getHeaders() });
  }

  getActiveSessions(): Observable<ActiveSession[]> {
    return this.http.get<ActiveSession[]>(`${this.apiUrl}/sessions/active`, { headers: this.getHeaders() });
  }

  getUserSessions(userId: string): Observable<ActiveSession[]> {
    return this.http.get<ActiveSession[]>(`${this.apiUrl}/sessions/user/${userId}`, { headers: this.getHeaders() });
  }

  getFailedLoginAttempts(top: number = 50): Observable<FailedLoginAttempt[]> {
    return this.http.get<FailedLoginAttempt[]>(`${this.apiUrl}/security/failed-attempts?top=${top}`, { headers: this.getHeaders() });
  }

  terminateSession(sessionId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/sessions/${sessionId}`, { headers: this.getHeaders() });
  }

  terminateUserSessions(userId: string): Observable<{ message: string; count: number }> {
    return this.http.delete<{ message: string; count: number }>(`${this.apiUrl}/sessions/user/${userId}`, { headers: this.getHeaders() });
  }
}

