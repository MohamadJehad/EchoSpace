import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersService } from '../../services/users.service';
import { SuggestedUser } from '../../interfaces';

@Component({
  selector: 'app-suggested-users',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './suggested-users.component.html',
  styleUrl: './suggested-users.component.css'
})
export class SuggestedUsersComponent implements OnInit {
  suggestedUsers: SuggestedUser[] = [];
  isLoading = false;

  constructor(private usersService: UsersService) {}

  ngOnInit(): void {
    this.loadSuggestedUsers();
  }

  loadSuggestedUsers(): void {
    this.isLoading = true;
    this.usersService.getSuggestedUsers(8).subscribe({
      next: (users) => {
        this.suggestedUsers = users.map(user => ({
          ...user,
          initials: this.getInitials(user.name || user.username || user.email)
        }));
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading suggested users:', error);
        this.isLoading = false;
        // Fallback to empty array
        this.suggestedUsers = [];
      }
    });
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    
    // If it's an email, use first letter
    if (name.includes('@')) {
      return name.charAt(0).toUpperCase();
    }
    
    // Split by space and get first letter of each word
    const words = name.trim().split(' ');
    if (words.length === 1) {
      return words[0].substring(0, 2).toUpperCase();
    }
    
    return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
  }

  onFollowUser(userId: string): void {
    // TODO: Implement follow functionality
    console.log('Follow user:', userId);
    // For now, just show a message
    alert('Follow functionality will be implemented soon!');
  }

  onRefresh(): void {
    this.loadSuggestedUsers();
  }

  trackByUserId(index: number, user: SuggestedUser): string {
    return user.id;
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }
}
