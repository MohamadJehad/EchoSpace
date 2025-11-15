import { Component, OnInit, HostListener, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { normalizeRole } from '../../utils/role.util';

interface UserData {
  name: string;
  email: string;
  initials: string;
  role?: any; // Can be string or number
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
    return normalizeRole(this.currentUser.role) === 'Admin';
  }

  get isOperation(): boolean {
    return normalizeRole(this.currentUser.role) === 'Operation';
  }
  
  @Output() logout = new EventEmitter<void>();
  
  showMenu = false;

  ngOnInit(): void {
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
