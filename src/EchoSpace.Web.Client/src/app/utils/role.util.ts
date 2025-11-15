/**
 * Utility function to normalize user role to string format
 * Handles both numeric enum values and string role names
 */
export function normalizeRole(role: any): string {
  if (!role) {
    return 'User';
  }

  // If it's already a string, return it
  if (typeof role === 'string') {
    return role;
  }

  // If it's a number, convert to string role name
  if (typeof role === 'number') {
    switch (role) {
      case 0:
        return 'User';
      case 1:
        return 'Admin';
      case 2:
        return 'Moderator';
      case 3:
        return 'Operation';
      default:
        return 'User';
    }
  }

  // If it's a string representation of a number
  if (typeof role === 'string' && !isNaN(Number(role))) {
    return normalizeRole(Number(role));
  }

  return 'User';
}

/**
 * Check if a role matches the allowed roles
 */
export function hasRole(userRole: any, allowedRoles: string[]): boolean {
  const normalizedRole = normalizeRole(userRole);
  return allowedRoles.includes(normalizedRole);
}

