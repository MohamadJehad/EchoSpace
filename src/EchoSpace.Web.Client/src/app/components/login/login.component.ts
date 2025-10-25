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
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const { email, password } = this.loginForm.value;
      
      this.authService.login({ email, password }).subscribe({
        next: (response) => {
          this.isLoading = false;
          
          // Redirect based on user role
          const userRole = response.user.role || 'User';
          if (userRole === 'Admin') {
            this.router.navigate(['/admin/users']);
          } else {
            this.router.navigate(['/home']);
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = 'Invalid credentials';
        }
      });
    } else {
      this.errorMessage = 'Please fill in all fields correctly';
    }
  }

  loginWithGoogle() {
    this.authService.googleLogin();
  }

  loginWithFacebook() {
    console.log('Login with Facebook');
    // TODO: Implement Facebook OAuth
  }
}

