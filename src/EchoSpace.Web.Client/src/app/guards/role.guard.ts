import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { normalizeRole, hasRole } from '../utils/role.util';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }

    // Get user from authService synchronously
    const user = authService.getCurrentUser();
    if (!user) {
      router.navigate(['/login']);
      return false;
    }

    const userRole = user.role;

    // Check if user has required role (handles both numeric and string)
    if (hasRole(userRole, allowedRoles)) {
      return true;
    }

    // User doesn't have permission, redirect based on their role
    const normalizedRole = normalizeRole(userRole);
    if (normalizedRole === 'Operation') {
      router.navigate(['/operation']);
    } else {
      router.navigate(['/home']);
    }
    return false;
  };
};

