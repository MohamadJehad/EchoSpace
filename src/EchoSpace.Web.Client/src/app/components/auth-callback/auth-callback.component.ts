import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './auth-callback.component.html',
  styleUrls: ['./auth-callback.component.css']
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const accessToken = params['accessToken'];
      const refreshToken = params['refreshToken'];
      const userStr = params['user'];

      if (accessToken && refreshToken && userStr) {
        try {
          const decodedUserStr = decodeURIComponent(userStr);
          const user = JSON.parse(decodedUserStr);
          
          // Create auth response object
          const authResponse: any = {
            accessToken: accessToken,
            refreshToken: refreshToken,
            expiresIn: 3600, // Default value
            user: user
          };
          
          // Use the public method to set session properly
          this.authService.setSessionFromCallback(authResponse);
          
          // Redirect to home
          this.router.navigate(['/']);
        } catch (error) {
          console.error('Error processing callback:', error);
          this.router.navigate(['/login']);
        }
      } else {
        // Missing parameters, redirect to login
        this.router.navigate(['/login']);
      }
    });
  }
}
