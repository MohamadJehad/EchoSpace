import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-email-verification',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './email-verification.component.html',
  styleUrls: ['./email-verification.component.css']
})
export class EmailVerificationComponent {
  @Input() email: string = '';
  @Output() verified = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();

  verificationForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  resendCooldown = 0;
  resendInterval: any;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService
  ) {
    this.verificationForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  ngOnInit() {
    this.sendVerificationCode();
  }

  ngOnDestroy() {
    if (this.resendInterval) {
      clearInterval(this.resendInterval);
    }
  }

  onSubmit() {
    if (this.verificationForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const { code } = this.verificationForm.value;
      
      this.authService.verifyEmail(this.email, code).subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = 'Email verified successfully!';
          setTimeout(() => {
            this.verified.emit();
          }, 1500);
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error.error?.message || 'Invalid verification code';
        }
      });
    } else {
      this.errorMessage = 'Please enter a valid 6-digit code';
    }
  }

  sendVerificationCode() {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    
    this.authService.sendEmailVerification(this.email).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Verification code sent to your email!';
        this.startResendCooldown();
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message || 'Failed to send verification code';
      }
    });
  }

  startResendCooldown() {
    this.resendCooldown = 60; // 60 seconds
    this.resendInterval = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0) {
        clearInterval(this.resendInterval);
      }
    }, 1000);
  }

  onBack() {
    this.back.emit();
  }
}
