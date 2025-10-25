import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  totpForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  showTotpVerification = false;
  showTotpSetup = false;
  userEmail = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(10)]]
    });

    this.totpForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const { email, password } = this.loginForm.value;
      this.userEmail = email;
      
      this.authService.login({ email, password }).subscribe({
        next: (response) => {
          this.isLoading = false;
          
          // Check if user needs to set up TOTP first
          if (response.requiresTotp) {
            this.showTotpVerification = true;
          } else {
            // This shouldn't happen with the new flow, but handle it gracefully
            this.router.navigate(['/home']);
          }
        },
        error: (error) => {
          this.isLoading = false;
          if (error.error?.message?.includes('TOTP is required') || error.error?.message?.includes('Please set up TOTP first')) {
            this.errorMessage = 'Your account needs TOTP setup. Please set up two-factor authentication.';
            this.showTotpSetup = true;
          } else {
            this.errorMessage = 'Invalid credentials';
          }
        }
      });
    } else {
      this.errorMessage = 'Please fill in all fields correctly';
    }
  }

  onTotpSubmit() {
    if (this.totpForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const { code } = this.totpForm.value;
      
      this.authService.verifyTotp(this.userEmail, code).subscribe({
        next: (response) => {
          this.isLoading = false;
          // Set session with the auth response
          this.authService.setSessionFromCallback(response);
          // Redirect based on user role
          const userRole = response.user?.role || 'User';
          if (userRole === 'Admin') {
            this.router.navigate(['/admin/users']);
          } else {
            this.router.navigate(['/home']);
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error.error?.message || 'Invalid TOTP code';
        }
      });
    } else {
      this.errorMessage = 'Please enter a valid 6-digit code';
    }
  }

  onBackToLogin() {
    this.showTotpVerification = false;
    this.showTotpSetup = false;
    this.totpForm.reset();
    this.errorMessage = '';
  }

  onTotpSetupCompleted() {
    // TOTP setup completed, now show verification
    this.showTotpSetup = false;
    this.showTotpVerification = true;
  }

  onTotpSetupBack() {
    // Go back to login form
    this.showTotpSetup = false;
    this.errorMessage = '';
  }

  loginWithGoogle() {
    this.authService.googleLogin();
  }

  loginWithFacebook() {
    console.log('Login with Facebook');
    // TODO: Implement Facebook OAuth
  }
}

