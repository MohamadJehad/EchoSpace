import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService, User } from '../../services/user.service';
import { NavbarComponent } from '../navbar/navbar.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

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
  changingRoleUserId: string | null = null;
  newRole: 'User' | 'Operation' | 'Admin' = 'User';
  currentUser: User | null = null;

  constructor(private userService: UserService) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadUsers();
  }

  loadCurrentUser(): void {
    this.userService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (err) => {
        // Fallback to localStorage if API call fails
        const userStr = localStorage.getItem('user');
        if (userStr) {
          try {
            this.currentUser = JSON.parse(userStr) as User;
          } catch {
            console.error('Failed to parse user from localStorage');
          }
        }
      }
    });
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

  changeUserRole(user: User): void {
    this.changingRoleUserId = user.id;
    this.newRole = (user.role as 'User' | 'Operation' | 'Admin') || 'User';
  }

  cancelRoleChange(): void {
    this.changingRoleUserId = null;
    this.newRole = 'User';
  }

  confirmRoleChange(): void {
    if (!this.changingRoleUserId) return;

    // Find the user being changed
    const targetUser = this.users.find(u => u.id === this.changingRoleUserId);
    if (!targetUser) return;

    // Validate permissions before making the request (only self-protection)
    if (!this.canChangeToRole(targetUser, this.newRole)) {
      this.error = 'You cannot change your own role.';
      this.isProcessing = false;
      return;
    }

    this.isProcessing = true;
    this.userService.changeUserRole(this.changingRoleUserId, this.newRole).subscribe({
      next: (updatedUser) => {
        this.error = null;
        const index = this.users.findIndex(u => u.id === updatedUser.id);
        if (index !== -1) {
          this.users[index] = { ...updatedUser };
        }
        this.isProcessing = false;
        this.changingRoleUserId = null;
        this.newRole = 'User';
      },
      error: (err) => {
        this.isProcessing = false;
        console.error('Failed to change user role', err);
        this.error = `Failed to change role: ${err.error?.message || err.message || 'Unknown error'}`;
      }
    });
  }

  getRoleBadgeClass(role?: string): string {
    switch (role) {
      case 'Admin':
        return 'bg-purple-100 text-purple-800';
      case 'Operation':
        return 'bg-blue-100 text-blue-800';
      case 'User':
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getRoleDisplayName(role?: string): string {
    return role || 'User';
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

    const handleError = (err: any) => {
      this.isProcessing = false;
      console.error(`Failed to ${action} user`, err);
      this.error = `Failed to ${action} user: ${err.error?.message || err.message || 'Unknown error'}`;
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
    }
  }

  onCancelModal(): void {
    this.showModal = false;
    this.pendingUserId = null;
    this.pendingAction = null;
    this.isProcessing = false;
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

  // Permission checks - Admin has full permissions per permission matrix
  canDeleteUser(user: User): boolean {
    if (!this.currentUser) return false;
    // Cannot delete yourself (safety measure)
    if (user.id === this.currentUser.id) {
      return false;
    }
    // Admin can delete any other user (including other admins)
    return true;
  }

  canChangeRole(user: User): boolean {
    if (!this.currentUser) return false;
    // Cannot change your own role (safety measure)
    if (user.id === this.currentUser.id) {
      return false;
    }
    // Admin can change any other user's role (including to Admin)
    return true;
  }

  canChangeToRole(user: User, targetRole: 'User' | 'Operation' | 'Admin'): boolean {
    if (!this.currentUser) return false;
    // Cannot change your own role (safety measure)
    if (user.id === this.currentUser.id) {
      return false;
    }
    // Admin can assign any role to any other user (including Admin)
    return true;
  }

  canLockUser(user: User): boolean {
    if (!this.currentUser) return false;
    // Cannot lock yourself (safety measure)
    if (user.id === this.currentUser.id) {
      return false;
    }
    // Admin can lock any other user (including other admins)
    return true;
  }

  canUnlockUser(user: User): boolean {
    if (!this.currentUser) return false;
    // Admin can unlock any user
    return true;
  }
}

