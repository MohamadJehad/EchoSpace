import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
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
        password: ['', [Validators.required, Validators.minLength(10), this.passwordComplexityValidator]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  // Custom validator for password complexity
  passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.value;
    if (!password) {
      return null;
    }

    const hasUpperCase = /[A-Z]/.test(password);
    const specialChars = /[@$!%*?#^~]/;
    const hasSpecialChar = specialChars.test(password);

    if (!hasUpperCase && !hasSpecialChar) {
      return { passwordComplexity: { message: 'Password must contain at least one uppercase letter and one special character (@$!%*?#^~).' } };
    } else if (!hasUpperCase) {
      return { passwordComplexity: { message: 'Password must contain at least one uppercase letter.' } };
    } else if (!hasSpecialChar) {
      return { passwordComplexity: { message: 'Password must contain at least one special character (@$!%*?#^~).' } };
    }

    return null;
  }

  hasSpecialChar(password: string | null | undefined): boolean {
    if (!password) return false;
    const specialChars = /[@$!%*?#^~]/;
    return specialChars.test(password);
  }

  hasUpperCase(password: string | null | undefined): boolean {
    if (!password) return false;
    return /[A-Z]/.test(password);
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
        console.log('Response type:', typeof response);
        console.log('Response isValid:', response?.isValid);
        console.log('Response message:', response?.message);
        this.isLoading = false;
        if (response && response.isValid) {
          this.isTokenValid = true;
          console.log('Token is valid, enabling form');
        } else {
          this.isTokenValid = false;
          this.errorMessage = response?.message || 'Invalid or expired reset token.';
          console.log('Token is invalid:', this.errorMessage);
        }
      },
      error: (error) => {
        console.error('Token validation error:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        console.error('Error body:', error.error);
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
      console.log('Form values:', { password: '***', confirmPassword: '***', token: this.token });
      console.log('Password length:', password?.length);
      console.log('Passwords match:', password === confirmPassword);

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
            console.error('Password reset error:', error);
            console.error('Error status:', error.status);
            console.error('Error body:', error.error);
            console.error('Validation errors:', error.error?.errors);
            this.isLoading = false;
            if (error.status === 400) {
              this.errorMessage = error.error?.message || 'Invalid or expired reset token.';
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
