import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UsersService } from '../../services/users.service';
import { FollowsService } from '../../services/follows.service';
import { SuggestedUser } from '../../interfaces';

@Component({
  selector: 'app-suggested-users',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './suggested-users.component.html',
  styleUrl: './suggested-users.component.css'
})
export class SuggestedUsersComponent implements OnInit {
  suggestedUsers: SuggestedUser[] = [];
  isLoading = false;
  followingStatus: { [userId: string]: boolean } = {};
  showAllModal = false;
  allSuggestedUsers: SuggestedUser[] = [];
  isLoadingAll = false;

  constructor(
    private usersService: UsersService,
    private followsService: FollowsService
  ) {}

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
        // Check follow status for each user
        users.forEach(user => {
          this.checkFollowStatus(user.id);
        });
      },
      error: (error) => {
        console.error('Error loading suggested users:', error);
        this.isLoading = false;
        // Fallback to empty array
        this.suggestedUsers = [];
      }
    });
  }

  checkFollowStatus(userId: string): void {
    this.followsService.getFollowStatus(userId).subscribe({
      next: (status) => {
        this.followingStatus[userId] = status.isFollowing;
      },
      error: (error) => {
        console.error('Error checking follow status:', error);
        this.followingStatus[userId] = false;
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
    const isFollowing = this.followingStatus[userId];
    
    const action = isFollowing 
      ? this.followsService.unfollowUser(userId)
      : this.followsService.followUser(userId);

    action.subscribe({
      next: () => {
        this.followingStatus[userId] = !isFollowing;
        // Remove user from suggested list if they just followed (not if unfollowing)
        if (!isFollowing) {
          this.suggestedUsers = this.suggestedUsers.filter(u => u.id !== userId);
        }
      },
      error: (error) => {
        console.error('Error following/unfollowing user:', error);
        alert('Failed to ' + (isFollowing ? 'unfollow' : 'follow') + ' user. Please try again.');
      }
    });
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

  onViewAll(): void {
    this.showAllModal = true;
    this.loadAllSuggestedUsers();
  }

  loadAllSuggestedUsers(): void {
    this.isLoadingAll = true;
    this.usersService.getSuggestedUsers(50).subscribe({
      next: (users) => {
        this.allSuggestedUsers = users.map(user => ({
          ...user,
          initials: this.getInitials(user.name || user.username || user.email)
        }));
        this.isLoadingAll = false;
        // Check follow status for each user (only if not already checked)
        users.forEach(user => {
          if (this.followingStatus[user.id] === undefined) {
            this.checkFollowStatusForAll(user.id);
          }
        });
      },
      error: (error) => {
        console.error('Error loading all suggested users:', error);
        this.isLoadingAll = false;
        this.allSuggestedUsers = [];
      }
    });
  }

  checkFollowStatusForAll(userId: string): void {
    this.followsService.getFollowStatus(userId).subscribe({
      next: (status) => {
        this.followingStatus[userId] = status.isFollowing;
      },
      error: (error) => {
        console.error('Error checking follow status:', error);
        this.followingStatus[userId] = false;
      }
    });
  }

  onFollowUserInModal(userId: string): void {
    const isFollowing = this.followingStatus[userId];
    
    const action = isFollowing 
      ? this.followsService.unfollowUser(userId)
      : this.followsService.followUser(userId);

    action.subscribe({
      next: () => {
        this.followingStatus[userId] = !isFollowing;
        // Update in both lists
        this.suggestedUsers.forEach(u => {
          if (u.id === userId) {
            // Update follow status
          }
        });
        this.allSuggestedUsers.forEach(u => {
          if (u.id === userId) {
            // Update follow status
          }
        });
        // Remove from suggested list if they just followed (not if unfollowing)
        if (!isFollowing) {
          this.suggestedUsers = this.suggestedUsers.filter(u => u.id !== userId);
          this.allSuggestedUsers = this.allSuggestedUsers.filter(u => u.id !== userId);
        }
      },
      error: (error) => {
        console.error('Error following/unfollowing user:', error);
        alert('Failed to ' + (isFollowing ? 'unfollow' : 'follow') + ' user. Please try again.');
      }
    });
  }

  closeModal(): void {
    this.showAllModal = false;
    this.allSuggestedUsers = [];
  }
}
