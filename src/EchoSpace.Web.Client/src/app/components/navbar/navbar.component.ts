import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  currentUser: any = null;
  isAdmin = false;
  canViewDashboard = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check localStorage first for immediate role detection
    this.checkRoleStatus();
    
    // Then subscribe to changes
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.isAdmin = this.checkAdminRole(user?.role);
      this.canViewDashboard = this.checkDashboardAccess(user?.role);
      
      // Also check localStorage as fallback
      if (!this.isAdmin || !this.canViewDashboard) {
        this.checkRoleStatus();
      }
    });
  }

  private checkAdminRole(role: any): boolean {
    if (!role) return false;
    // Check for string "Admin"
    if (role === 'Admin' || role === 'admin') return true;
    // Check for number 2 (Admin enum value)
    if (role === 2 || role === '2') return true;
    return false;
  }
  
  private checkDashboardAccess(role: any): boolean {
    if (!role) return false;
    // Check for Admin
    if (role === 'Admin' || role === 'admin' || role === 2 || role === '2') return true;
    // Check for Operation
    if (role === 'Operation' || role === 'operation' || role === 1 || role === '1') return true;
    return false;
  }

  private checkRoleStatus(): void {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        this.isAdmin = this.checkAdminRole(user?.role);
        this.canViewDashboard = this.checkDashboardAccess(user?.role);
        if (user && !this.currentUser) {
          this.currentUser = user;
        }
      } catch (e) {
        console.error('Error parsing user from localStorage', e);
      }
    }
  }

  get currentUser$() {
    return this.authService.currentUser$;
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
