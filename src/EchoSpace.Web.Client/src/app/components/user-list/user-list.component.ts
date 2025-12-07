import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService, User } from '../../services/user.service';
import { NavbarComponent } from '../navbar/navbar.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { normalizeRole } from '../../utils/role.util';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, NavbarComponent, ConfirmationModalComponent],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css']
})
export class UserListComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error: string | null = null;
  showModal = false;
  pendingUserId: string | null = null;
  pendingAction: 'delete' | 'lock' | 'unlock' | 'changeRole' | null = null;
  isProcessing = false;
  
  // Role change
  editingRoleUserId: string | null = null;
  editingRoleValue: number | null = null;
  
  // Available roles
  roles = [
    { value: 0, label: 'User' },
    { value: 1, label: 'Admin' },
    { value: 2, label: 'Moderator' },
    { value: 3, label: 'Operation' }
  ];

  constructor(private userService: UserService) { }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;
    
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load users';
        this.loading = false;
        console.error(err);
      }
    });
  }

  deleteUser(id: string): void {
    this.pendingUserId = id;
    this.pendingAction = 'delete';
    this.showModal = true;
  }

  lockUser(id: string): void {
    this.pendingUserId = id;
    this.pendingAction = 'lock';
    this.showModal = true;
  }

  unlockUser(id: string): void {
    this.pendingUserId = id;
    this.pendingAction = 'unlock';
    this.showModal = true;
  }

  changeUserRole(userId: string, newRole: number): void {
    this.pendingUserId = userId;
    this.pendingAction = 'changeRole';
    this.editingRoleValue = newRole;
    this.showModal = true;
  }

  startEditRole(userId: string): void {
    const user = this.users.find(u => u.id === userId);
    if (user) {
      // Get role as number (handle both string and number)
      let roleValue = 0;
      if (typeof user.role === 'number') {
        roleValue = user.role;
      } else if (typeof user.role === 'string') {
        const roleMap: { [key: string]: number } = {
          'User': 0,
          'Admin': 1,
          'Moderator': 2,
          'Operation': 3
        };
        roleValue = roleMap[user.role] ?? 0;
      }
      this.editingRoleUserId = userId;
      this.editingRoleValue = roleValue;
    }
  }

  cancelEditRole(): void {
    this.editingRoleUserId = null;
    this.editingRoleValue = null;
  }

  saveRoleChange(userId: string): void {
    if (this.editingRoleValue === null || this.editingRoleValue === undefined) return;
    
    this.pendingUserId = userId;
    this.pendingAction = 'changeRole';
    this.showModal = true;
  }

  onConfirm(): void {
    if (!this.pendingUserId || !this.pendingAction) return;

    this.isProcessing = true;
    const userId = this.pendingUserId;
    const action = this.pendingAction;

    const handleSuccess = (updatedUser?: User) => {
      // Clear any previous errors
      this.error = null;
      
      // Update the local user object if we got an updated user back
      if (updatedUser) {
        const index = this.users.findIndex(u => u.id === updatedUser.id);
        if (index !== -1) {
          // Create a new object reference to trigger Angular change detection
          this.users[index] = { ...updatedUser };
        }
      }
      
      // Always reload to ensure consistency with the server
      this.loadUsers();
      
      this.isProcessing = false;
      this.showModal = false;
      this.pendingUserId = null;
      this.pendingAction = null;
    };

    const handleError = (err: unknown) => {
      this.isProcessing = false;
      console.error(`Failed to ${action} user`, err);
      // Extract validation errors if present
      let errorMessage = 'Unknown error';
      if (err && typeof err === 'object') {
        if ('error' in err && err.error && typeof err.error === 'object') {
          if ('message' in err.error) {
            errorMessage = String(err.error.message);
          }
          if ('errors' in err.error && err.error.errors && typeof err.error.errors === 'object') {
            const validationErrors = Object.entries(err.error.errors)
              .map(([key, value]) => `${key}: ${Array.isArray(value) ? value.join(', ') : String(value)}`)
              .join('; ');
            errorMessage = `Validation errors: ${validationErrors}`;
          }
        } else if ('message' in err) {
          errorMessage = String(err.message);
        }
      }
      this.error = `Failed to ${action} user: ${errorMessage}`;
      this.showModal = false;
      this.pendingUserId = null;
      this.pendingAction = null;
    };

    if (action === 'delete') {
      this.userService.deleteUser(userId).subscribe({ 
        next: () => handleSuccess(), 
        error: handleError 
      });
    } else if (action === 'lock') {
      this.userService.lockUser(userId).subscribe({ 
        next: (user) => handleSuccess(user), 
        error: handleError 
      });
    } else if (action === 'unlock') {
      this.userService.unlockUser(userId).subscribe({ 
        next: (user) => handleSuccess(user), 
        error: handleError 
      });
    } else if (action === 'changeRole') {
      if (this.editingRoleValue === null || this.editingRoleValue === undefined) {
        handleError({ message: 'Role value is required' });
        return;
      }
      // Update user role only - ensure it's sent as a number
      const roleValue = typeof this.editingRoleValue === 'string' 
        ? parseInt(this.editingRoleValue, 10) 
        : this.editingRoleValue;
      this.userService.updateUser(userId, { 
        role: roleValue 
      }).subscribe({
        next: (user) => {
          handleSuccess(user);
          this.editingRoleUserId = null;
          this.editingRoleValue = null;
        },
        error: handleError
      });
    }
  }

  onCancelModal(): void {
    this.showModal = false;
    this.pendingUserId = null;
    this.pendingAction = null;
    this.isProcessing = false;
    this.editingRoleUserId = null;
    this.editingRoleValue = null;
  }

  getModalConfig() {
    switch (this.pendingAction) {
      case 'delete':
        return {
          title: 'Delete User',
          message: 'Are you sure you want to delete this user? This action cannot be undone.',
          confirmText: 'Delete',
          buttonClass: 'bg-red-600 hover:bg-red-700'
        };
      case 'lock':
        return {
          title: 'Lock User Account',
          message: 'Are you sure you want to lock this user account? The user will not be able to log in until the account is unlocked.',
          confirmText: 'Lock Account',
          buttonClass: 'bg-yellow-600 hover:bg-yellow-700'
        };
      case 'unlock':
        return {
          title: 'Unlock User Account',
          message: 'Are you sure you want to unlock this user account? The user will be able to log in again.',
          confirmText: 'Unlock Account',
          buttonClass: 'bg-green-600 hover:bg-green-700'
        };
      case 'changeRole':
        const roleLabel = this.roles.find(r => r.value === this.editingRoleValue)?.label || 'Unknown';
        return {
          title: 'Change User Role',
          message: `Are you sure you want to change this user's role to ${roleLabel}? This will affect their access permissions.`,
          confirmText: 'Change Role',
          buttonClass: 'bg-blue-600 hover:bg-blue-700'
        };
      default:
        return {
          title: 'Confirm Action',
          message: 'Are you sure?',
          confirmText: 'Confirm',
          buttonClass: 'bg-blue-600 hover:bg-blue-700'
        };
    }
  }

  isAccountLocked(user: User): boolean {
    if (!user.lockoutEnabled || !user.lockoutEnd) {
      return false;
    }
    const lockoutEnd = new Date(user.lockoutEnd);
    return lockoutEnd > new Date();
  }

  getLockoutStatus(user: User): string {
    if (!user.lockoutEnabled) {
      return 'Disabled';
    }
    if (!user.lockoutEnd) {
      return 'Active';
    }
    const lockoutEnd = new Date(user.lockoutEnd);
    if (lockoutEnd > new Date()) {
      return `Locked until ${lockoutEnd.toLocaleString()}`;
    }
    return 'Active';
  }

  getUserRole(user: User): string {
    return normalizeRole(user.role);
  }

  getUserRoleValue(user: User): number {
    if (typeof user.role === 'number') {
      return user.role;
    }
    if (typeof user.role === 'string') {
      const roleMap: { [key: string]: number } = {
        'User': 0,
        'Admin': 1,
        'Moderator': 2,
        'Operation': 3
      };
      return roleMap[user.role] ?? 0;
    }
    return 0;
  }
}

