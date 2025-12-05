import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpErrorResponse } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const token = this.authService.getToken();
    
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        // Don't try to refresh if the request is to the refresh endpoint itself
        if (error.status === 401 && !req.url.includes('/auth/refresh')) {
          // Prevent multiple simultaneous refresh attempts
          if (!this.isRefreshing) {
            this.isRefreshing = true;
            
            // Try to refresh the token
            return this.authService.refreshToken().pipe(
              switchMap(() => {
                this.isRefreshing = false;
                const newToken = this.authService.getToken();
                if (newToken) {
                  req = req.clone({
                    setHeaders: {
                      Authorization: `Bearer ${newToken}`
                    }
                  });
                  return next.handle(req);
                } else {
                  // No token after refresh, logout
                  this.authService.logout();
                  this.router.navigate(['/login']);
                  return throwError(() => error);
                }
              }),
              catchError((refreshError) => {
                this.isRefreshing = false;
                // Refresh failed, logout user
                this.authService.logout();
                this.router.navigate(['/login']);
                return throwError(() => refreshError);
              })
            );
          } else {
            // Already refreshing, wait a bit and retry original request
            return throwError(() => error);
          }
        }
        
        // If it's a 401 on the refresh endpoint or other errors, just throw
        return throwError(() => error);
      })
    );
  }
}

