import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Toast {
  id: string;
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  public toasts$ = this.toastsSubject.asObservable();

  private showToast(toast: Omit<Toast, 'id'>): void {
    const id = this.generateId();
    const duration = toast.duration ?? 5000; // Use nullish coalescing to ensure it's always a number
    const newToast: Toast = {
      ...toast,
      id,
      duration
    };

    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next([...currentToasts, newToast]);

    // Auto remove toast after duration
    if (duration > 0) {
      setTimeout(() => {
        this.removeToast(id);
      }, duration);
    }
  }

  success(title: string, message: string, duration?: number): void {
    this.showToast({ type: 'success', title, message, duration });
  }

  error(title: string, message: string, duration?: number): void {
    this.showToast({ type: 'error', title, message, duration });
  }

  info(title: string, message: string, duration?: number): void {
    this.showToast({ type: 'info', title, message, duration });
  }

  warning(title: string, message: string, duration?: number): void {
    this.showToast({ type: 'warning', title, message, duration });
  }

  removeToast(id: string): void {
    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next(currentToasts.filter(toast => toast.id !== id));
  }

  clearAll(): void {
    this.toastsSubject.next([]);
  }

  private generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }
}
