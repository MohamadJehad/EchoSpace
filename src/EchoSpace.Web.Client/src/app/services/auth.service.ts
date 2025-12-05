import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError } from 'rxjs';
import { environment } from '../../environments/environment';
import { CookieService } from './cookie.service';

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

  constructor(
    private http: HttpClient,
    private cookieService: CookieService
  ) {
    // Load user from cookies on init
    const token = this.cookieService.get('accessToken');
    const user = this.cookieService.get('user');
    if (token && user) {
      try {
        this.currentUserSubject.next(JSON.parse(user));
      } catch (e) {
        // Invalid JSON, clear it
        this.clearSession();
      }
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
    const refreshToken = this.cookieService.get('refreshToken');
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe();
    }
    this.clearSession();
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.cookieService.get('refreshToken');
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(tap((response) => this.setSession(response)));
  }

  isAuthenticated(): boolean {
    const token = this.cookieService.get('accessToken');
    return !!token && !this.isTokenExpired();
  }

  private setSession(authResult: AuthResponse): void {
    // Store in cookies instead of localStorage
    // Access token: 15 minutes (0.01 days ≈ 15 minutes)
    this.cookieService.set('accessToken', authResult.accessToken, 0.01);
    
    // Refresh token: 7 days
    this.cookieService.set('refreshToken', authResult.refreshToken, 7);
    
    // User data: 7 days (same as refresh token)
    this.cookieService.set('user', JSON.stringify(authResult.user), 7);
    
    this.currentUserSubject.next(authResult.user);
  }

  private clearSession(): void {
    // Delete cookies instead of localStorage
    this.cookieService.delete('accessToken');
    this.cookieService.delete('refreshToken');
    this.cookieService.delete('user');
    this.currentUserSubject.next(null);
  }

  private isTokenExpired(): boolean {
    const token = this.cookieService.get('accessToken');
    if (!token) return true;

    try {
      const expiry = JSON.parse(atob(token.split('.')[1])).exp;
      return Math.floor(Date.now() / 1000) >= expiry;
    } catch {
      return true;
    }
  }

  getToken(): string | null {
    return this.cookieService.get('accessToken');
  }

  // Get current user synchronously from cookies
  getCurrentUser(): any | null {
    const userStr = this.cookieService.get('user');
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
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

  completeRegistration(email: string, code: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/complete-registration`, { email, code });
  }
}
