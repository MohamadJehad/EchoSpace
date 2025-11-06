import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UserService, User } from '../../services/user.service';
import { NavbarComponent } from '../navbar/navbar.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css']
})
export class UserListComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error: string | null = null;

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
    if (confirm('Are you sure you want to delete this user?')) {
      this.userService.deleteUser(id).subscribe({
        next: () => {
          this.loadUsers(); // Reload the list
        },
        error: (err) => {
          console.error('Failed to delete user', err);
          alert('Failed to delete user');
        }
      });
    }
  }

  lockUser(id: string): void {
    if (confirm('Are you sure you want to lock this user account?')) {
      this.userService.lockUser(id).subscribe({
        next: () => {
          this.loadUsers(); // Reload the list
        },
        error: (err) => {
          console.error('Failed to lock user', err);
          alert('Failed to lock user account');
        }
      });
    }
  }

  unlockUser(id: string): void {
    if (confirm('Are you sure you want to unlock this user account?')) {
      this.userService.unlockUser(id).subscribe({
        next: () => {
          this.loadUsers(); // Reload the list
        },
        error: (err) => {
          console.error('Failed to unlock user', err);
          alert('Failed to unlock user account');
        }
      });
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

