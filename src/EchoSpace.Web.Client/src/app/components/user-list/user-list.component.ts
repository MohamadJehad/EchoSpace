import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UserService, User } from '../../services/user.service';
import { NavbarComponent } from '../navbar/navbar.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent, ConfirmationModalComponent],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css']
})
export class UserListComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error: string | null = null;
  showModal = false;
  pendingUserId: string | null = null;
  pendingAction: 'delete' | 'lock' | 'unlock' | null = null;
  isProcessing = false;

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
}

