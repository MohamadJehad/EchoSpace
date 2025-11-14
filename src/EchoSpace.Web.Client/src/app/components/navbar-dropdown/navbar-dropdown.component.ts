import { Component, OnInit, HostListener, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface UserData {
  name: string;
  email: string;
  initials: string;
  role?: string;
  profilePhotoUrl?: string | null;
}

@Component({
  selector: 'app-navbar-dropdown',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar-dropdown.component.html',
  styleUrl: './navbar-dropdown.component.css'
})
export class NavbarDropdownComponent implements OnInit {
  @Input() currentUser: UserData = {
    name: 'User',
    email: '',
    initials: 'U',
    role: 'User'
  };

  get isAdmin(): boolean {
    // Helper function to check if role is Admin
    const checkRole = (role: any): boolean => {
      if (!role) return false;
      // Check for string "Admin"
      if (role === 'Admin' || role === 'admin') return true;
      // Check for number 2 (Admin enum value)
      if (role === 2 || role === '2') return true;
      return false;
    };

    // Check currentUser first
    if (checkRole(this.currentUser?.role)) {
      return true;
    }
    
    // Fallback: Check localStorage
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        if (checkRole(user?.role)) {
          return true;
        }
      } catch (e) {
        console.error('Error parsing user from localStorage', e);
      }
    }
    
    return false;
  }
  
  get canViewDashboard(): boolean {
    // Helper function to check if role is Admin or Operation
    const checkRole = (role: any): boolean => {
      if (!role) return false;
      // Check for Admin
      if (role === 'Admin' || role === 'admin' || role === 2 || role === '2') return true;
      // Check for Operation
      if (role === 'Operation' || role === 'operation' || role === 1 || role === '1') return true;
      return false;
    };

    // Check currentUser first
    if (checkRole(this.currentUser?.role)) {
      return true;
    }
    
    // Fallback: Check localStorage
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        if (checkRole(user?.role)) {
          return true;
        }
      } catch (e) {
        console.error('Error parsing user from localStorage', e);
      }
    }
    
    return false;
  }
  
  @Output() logout = new EventEmitter<void>();
  
  showMenu = false;

  ngOnInit(): void {
    // Ensure role is loaded from localStorage if not in currentUser
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        if (user?.role) {
          // Update currentUser with role from localStorage
          this.currentUser = { ...this.currentUser, role: user.role };
        }
      } catch (e) {
        console.error('Error parsing user from localStorage', e);
      }
    }
  }

  toggleMenu(): void {
    this.showMenu = !this.showMenu;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const dropdownElement = target.closest('.relative');
    
    // Close menu if clicking outside
    if (!dropdownElement || !dropdownElement.querySelector('button')) {
      this.showMenu = false;
    }
  }

  onLogout(): void {
    this.logout.emit();
  }
}
