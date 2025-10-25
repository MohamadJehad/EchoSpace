import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    id: string;
    name: string;
    email: string;
    username?: string;
    role?: string;
  };
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    // Load user from localStorage on init
    const token = localStorage.getItem('accessToken');
    const user = localStorage.getItem('user');
    if (token && user) {
      this.currentUserSubject.next(JSON.parse(user));
    }
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, credentials)
      .pipe(tap((response) => this.setSession(response)));
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, data)
      .pipe(tap((response) => this.setSession(response)));
  }

  googleLogin(): void {
    // Redirect to backend Google OAuth endpoint
    window.location.href = `${this.apiUrl}/google`;
  }

  logout(): void {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe();
    }
    this.clearSession();
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(tap((response) => this.setSession(response)));
  }

  isAuthenticated(): boolean {
    const token = localStorage.getItem('accessToken');
    return !!token && !this.isTokenExpired();
  }

  private setSession(authResult: AuthResponse): void {
    localStorage.setItem('accessToken', authResult.accessToken);
    localStorage.setItem('refreshToken', authResult.refreshToken);
    localStorage.setItem('user', JSON.stringify(authResult.user));
    this.currentUserSubject.next(authResult.user);
  }

  private clearSession(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  private isTokenExpired(): boolean {
    const token = localStorage.getItem('accessToken');
    if (!token) return true;

    try {
      const expiry = JSON.parse(atob(token.split('.')[1])).exp;
      return Math.floor(Date.now() / 1000) >= expiry;
    } catch {
      return true;
    }
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  // Public method to set session from OAuth callback
  setSessionFromCallback(authResult: AuthResponse): void {
    this.setSession(authResult);
  }

  // Password reset methods
  forgotPassword(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email });
  }

  validateResetToken(token: string): Observable<any> {
    console.log('Sending token to backend:', token);
    console.log('Token length:', token?.length);
    const requestBody = { token };
    console.log('Request body:', requestBody);
    return this.http.post(`${this.apiUrl}/validate-reset-token`, requestBody);
  }

  resetPassword(
    token: string,
    newPassword: string,
    confirmPassword: string
  ): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, {
      token,
      newPassword,
      confirmPassword,
    });
  }
}
