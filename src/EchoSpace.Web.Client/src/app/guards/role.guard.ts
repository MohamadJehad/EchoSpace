import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }

    // Get user from localStorage
    const userStr = localStorage.getItem('user');
    if (!userStr) {
      router.navigate(['/login']);
      return false;
    }

    const user = JSON.parse(userStr);
    const userRole = user.role || 'User';

    // Check if user has required role
    if (allowedRoles.includes(userRole)) {
      return true;
    }

    // User doesn't have permission, redirect to home
    router.navigate(['/home']);
    return false;
  };
};

