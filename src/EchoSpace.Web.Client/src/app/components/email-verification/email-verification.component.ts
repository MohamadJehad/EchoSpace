import { Component, Input, Output, EventEmitter, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, AuthResponse } from '../../services/auth.service';

@Component({
  selector: 'app-email-verification',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './email-verification.component.html',
  styleUrls: ['./email-verification.component.css']
})
export class EmailVerificationComponent implements OnDestroy {
  @Input() email: string = '';
  @Input() isRegistration: boolean = false;
  @Output() verified = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();

  verificationForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  resendCooldown = 0;
  resendInterval: NodeJS.Timeout | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService
  ) {
    this.verificationForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  ngOnInit() {
    // Only send email if this is NOT a registration flow
    // Registration already sends the email in RegisterAsync
    if (!this.isRegistration) {
      this.sendVerificationCode();
    }
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
      
      // For registration flow, use completeRegistration to get tokens
      // For other flows (like password reset), use verifyEmail
      const verificationMethod = this.isRegistration ? 
        this.authService.completeRegistration(this.email, code) :
        this.authService.verifyEmail(this.email, code);

      verificationMethod.subscribe({
        next: (response: AuthResponse | unknown) => {
          this.isLoading = false;
          this.successMessage = 'Email verified successfully!';
          
          // If this is registration flow and response contains tokens, set session
          if (this.isRegistration && response && typeof response === 'object' && 'accessToken' in response && 'refreshToken' in response) {
            this.authService.setSessionFromCallback(response as AuthResponse);
          }
          
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
