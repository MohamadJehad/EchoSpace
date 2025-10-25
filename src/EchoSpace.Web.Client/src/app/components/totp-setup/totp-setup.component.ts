import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, TotpSetupResponse } from '../../services/auth.service';

@Component({
  selector: 'app-totp-setup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './totp-setup.component.html',
  styleUrls: ['./totp-setup.component.css']
})
export class TotpSetupComponent {
  @Input() email: string = '';
  @Output() completed = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();

  totpForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  totpData: TotpSetupResponse | null = null;
  showManualEntry = false;
  currentStep = 1; // 1: Setup, 2: Verify

  constructor(
    private fb: FormBuilder,
    private authService: AuthService
  ) {
    this.totpForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  ngOnInit() {
    this.setupTotp();
  }

  setupTotp() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.authService.setupTotp(this.email).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.totpData = response;
        this.currentStep = 1;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message || 'Failed to setup TOTP';
      }
    });
  }

  onVerifyTotp() {
    if (this.totpForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      const { code } = this.totpForm.value;
      
      this.authService.verifyTotp(this.email, code).subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = 'TOTP setup completed successfully!';
          setTimeout(() => {
            this.completed.emit();
          }, 1500);
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

  onBack() {
    this.back.emit();
  }

  toggleManualEntry() {
    this.showManualEntry = !this.showManualEntry;
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      // You could add a toast notification here
      console.log('Copied to clipboard');
    });
  }
}
