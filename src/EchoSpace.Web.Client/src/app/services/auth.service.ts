import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError } from 'rxjs';
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
  requiresTotp?: boolean;
  user: {
    id: string;
    name: string;
    email: string;
    username?: string;
    role?: string;
  };
}

export interface TotpSetupRequest {
  email: string;
}

export interface TotpSetupResponse {
  qrCodeUrl: string;
  secretKey: string;
  manualEntryKey: string;
}

export interface TotpVerificationRequest {
  email: string;
  code: string;
}

export interface EmailVerificationRequest {
  email: string;
  code: string;
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
    console.log('API URL:', `${this.apiUrl}/validate-reset-token`);
    
    return this.http.post(`${this.apiUrl}/validate-reset-token`, requestBody).pipe(
      tap(response => {
        console.log('Raw HTTP response:', response);
        console.log('Response type:', typeof response);
      }),
      catchError(error => {
        console.error('HTTP error:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        console.error('Error body:', error.error);
        throw error;
      })
    );
  }

  resetPassword(
    token: string,
    newPassword: string,
    confirmPassword: string
  ): Observable<any> {
    const requestBody = {
      token,
      newPassword,
      confirmPassword: confirmPassword, // Backend expects ConfirmPassword with capital C
    };
    
    console.log('Reset password request:', {
      token: token?.substring(0, 10) + '...',
      newPassword: '***',
      confirmPassword: '***',
      passwordLength: newPassword?.length,
      passwordsMatch: newPassword === confirmPassword
    });
    
    return this.http.post(`${this.apiUrl}/reset-password`, requestBody);
  }

  // TOTP Methods
  setupTotp(email: string): Observable<TotpSetupResponse> {
    return this.http.post<TotpSetupResponse>(`${this.apiUrl}/setup-totp`, { email });
  }

  verifyTotp(email: string, code: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/verify-totp`, { email, code });
  }

  sendEmailVerification(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/send-email-verification`, { email });
  }

  verifyEmail(email: string, code: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/verify-email`, { email, code });
  }

  setupTotpForExistingUser(email: string): Observable<TotpSetupResponse> {
    return this.http.post<TotpSetupResponse>(`${this.apiUrl}/setup-totp-for-existing-user`, { email });
  }
}
