import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserService } from '../../services/user.service';
import { ToastService } from '../../services/toast.service';

export interface UserData {
  name: string;
  email: string;
  initials: string;
  role?: string;
  id: string;
  profilePhotoUrl: string | null;
}

export interface UserStats {
  postsCount: number;
  followersCount: number;
  followingCount: number;
}

@Component({
  selector: 'app-profile-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-card.component.html',
  styleUrl: './profile-card.component.css'
})
export class ProfileCardComponent {
  @Input() currentUser!: UserData;
  @Input() userStats!: UserStats;
  @Input() isLoadingStats = false;
  
  @Output() profilePhotoUpdated = new EventEmitter<string>(); // Emits the new profile photo URL

  // Profile photo upload
  isUploadingPhoto = false;
  profilePhotoFile: File | null = null;
  profilePhotoPreview: string | null = null;

  constructor(
    private userService: UserService,
    private toastService: ToastService
  ) {}

  formatCount(count: number): string {
    if (count >= 1000) {
      return (count / 1000).toFixed(1) + 'K';
    }
    return count.toString();
  }

  onProfilePhotoClick(): void {
    setTimeout(() => {
      const fileInput = document.getElementById('profilePhotoInput') as HTMLInputElement;
      if (fileInput) {
        fileInput.click();
      } else {
        console.error('Profile photo input not found');
      }
    }, 0);
  }

  onProfilePhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      
      if (!file.type.startsWith('image/')) {
        this.toastService.error('Error', 'Please select an image file');
        return;
      }
      
      if (file.size > 10 * 1024 * 1024) {
        this.toastService.error('Error', 'File size must be less than 10MB');
        return;
      }
      
      this.profilePhotoFile = file;
      
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result && typeof e.target.result === 'string') {
          this.profilePhotoPreview = e.target.result;
        }
      };
      reader.readAsDataURL(file);
    }
  }

  uploadProfilePhoto(): void {
    if (!this.profilePhotoFile) {
      this.toastService.error('Error', 'Please select a photo first');
      return;
    }
    
    this.isUploadingPhoto = true;
    
    this.userService.uploadProfilePhoto(this.profilePhotoFile).subscribe({
      next: (response) => {
        console.log('uploadProfilePhoto: Upload response:', response);
        
        // Use the imageUrl from response immediately for display
        if (response.imageUrl) {
          console.log('uploadProfilePhoto: Setting profile photo URL from response:', response.imageUrl);
          this.currentUser.profilePhotoUrl = response.imageUrl;
          
          // Emit event to parent component
          this.profilePhotoUpdated.emit(response.imageUrl);
        } else {
          console.warn('uploadProfilePhoto: No imageUrl in response:', response);
        }
        
        // Clear preview and file AFTER we've set the URL
        this.profilePhotoPreview = null;
        this.profilePhotoFile = null;
        this.isUploadingPhoto = false;
        
        this.toastService.success('Success!', 'Profile photo uploaded successfully');
        
        const fileInput = document.getElementById('profilePhotoInput') as HTMLInputElement;
        if (fileInput) {
          fileInput.value = '';
        }
      },
      error: (error) => {
        console.error('Error uploading profile photo:', error);
        this.isUploadingPhoto = false;
        this.toastService.error('Error', error.error?.message || 'Failed to upload profile photo');
      }
    });
  }

  cancelProfilePhotoUpload(): void {
    this.profilePhotoFile = null;
    this.profilePhotoPreview = null;
    const fileInput = document.getElementById('profilePhotoInput') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }
}

