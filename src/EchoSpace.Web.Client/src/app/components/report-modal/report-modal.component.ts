import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-report-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-modal.component.html',
  styleUrl: './report-modal.component.css'
})
export class ReportModalComponent {
  @Input() isOpen = false;
  @Input() isLoading = false;

  @Output() confirm = new EventEmitter<string>();
  @Output() cancel = new EventEmitter<void>();

  reason: string = '';

  onConfirm(): void {
    this.confirm.emit(this.reason.trim() || undefined);
  }

  onCancel(): void {
    this.reason = '';
    this.cancel.emit();
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }
}

