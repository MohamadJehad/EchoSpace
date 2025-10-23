import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
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
      
      // TODO: Implement actual registration logic with your auth service
      const { name, email, password } = this.registerForm.value;
      console.log('Registration attempt:', { name, email, password });
      
      // Simulate API call
      setTimeout(() => {
        this.isLoading = false;
        // For now, just navigate to login on success
        this.router.navigate(['/login']);
      }, 1000);
    } else {
      this.errorMessage = 'Please fill in all fields correctly';
    }
  }

  registerWithGoogle() {
    console.log('Register with Google');
    // TODO: Implement Google OAuth
  }

  registerWithFacebook() {
    console.log('Register with Facebook');
    // TODO: Implement Facebook OAuth
  }
}

