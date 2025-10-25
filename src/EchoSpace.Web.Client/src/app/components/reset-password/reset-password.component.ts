import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css'],
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  token = '';
  isTokenValid = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService
  ) {
    this.resetPasswordForm = this.fb.group(
      {
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  ngOnInit() {
    // Get token from URL parameters
    this.route.queryParams.subscribe((params) => {
      this.token = params['token'];
      if (this.token) {
        this.validateToken();
      } else {
        this.errorMessage = 'Invalid or missing reset token.';
      }
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (
      password &&
      confirmPassword &&
      password.value !== confirmPassword.value
    ) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }

    return null;
  }

  validateToken() {
    this.isLoading = true;
    console.log('Validating token:', this.token);
    console.log('Token length:', this.token?.length);
    console.log('Token type:', typeof this.token);
    this.authService.validateResetToken(this.token).subscribe({
      next: (response) => {
        console.log('Token validation response:', response);
        this.isLoading = false;
        this.isTokenValid = true;
      },
      error: (error) => {
        console.error('Token validation error:', error);
        this.isLoading = false;
        this.isTokenValid = false;
        this.errorMessage =
          'Invalid or expired reset token. Please request a new password reset.';
      },
    });
  }

  onSubmit() {
    if (this.resetPasswordForm.valid && this.isTokenValid) {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      const { password, confirmPassword } = this.resetPasswordForm.value;
      // const {confirmPassword} = this.resetPasswordForm.value;

      this.authService
        .resetPassword(this.token, password, confirmPassword)
        .subscribe({
          next: (response) => {
            this.isLoading = false;
            this.successMessage =
              'Your password has been successfully reset. You can now sign in with your new password.';
            setTimeout(() => {
              this.router.navigate(['/login']);
            }, 3000);
          },
          error: (error) => {
            this.isLoading = false;
            if (error.status === 400) {
              this.errorMessage = 'Invalid or expired reset token.';
            } else {
              this.errorMessage = 'An error occurred. Please try again later.';
            }
          },
        });
    } else {
      this.errorMessage = 'Please fill in all fields correctly.';
    }
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  goToForgotPassword() {
    this.router.navigate(['/forgot-password']);
  }
}
