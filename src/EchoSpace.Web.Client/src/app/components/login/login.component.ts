import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

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
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      // TODO: Implement actual login logic with your auth service
      const { email, password } = this.loginForm.value;
      console.log('Login attempt:', { email, password });
      
      // Simulate API call
      setTimeout(() => {
        this.isLoading = false;
        // For now, just navigate to home on success
        this.router.navigate(['/']);
      }, 1000);
    } else {
      this.errorMessage = 'Please fill in all fields correctly';
    }
  }

  loginWithGoogle() {
    console.log('Login with Google');
    // TODO: Implement Google OAuth
  }

  loginWithFacebook() {
    console.log('Login with Facebook');
    // TODO: Implement Facebook OAuth
  }
}

