import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TotpSetupComponent } from '../totp-setup/totp-setup.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TotpSetupComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  currentStep = 1; // 1: Registration, 2: TOTP Setup
  userEmail = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.registerForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(10)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');
    
    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }

  onSubmit() {
    if (this.registerForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const { name, email, password } = this.registerForm.value;
      this.userEmail = email;
      
      this.authService.register({ name, email, password }).subscribe({
        next: () => {
          this.isLoading = false;
          // Move directly to TOTP setup step
          this.currentStep = 2;
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error.error?.message || 'Registration failed';
        }
      });
    } else {
      this.errorMessage = 'Please fill in all fields correctly';
    }
  }

  onTotpCompleted() {
    // Registration complete, redirect to home
    this.router.navigate(['/']);
  }

  onTotpBack() {
    // Go back to registration step
    this.currentStep = 1;
  }

  registerWithGoogle() {
    this.authService.googleLogin();
  }

  registerWithFacebook() {
    console.log('Register with Facebook');
    // TODO: Implement Facebook OAuth
  }
}

