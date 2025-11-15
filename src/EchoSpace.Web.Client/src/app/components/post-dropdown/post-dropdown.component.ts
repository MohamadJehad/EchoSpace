import { Component, Input, Output, EventEmitter, HostListener, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Post } from '../../interfaces';
import { FollowsService } from '../../services/follows.service';
import { PostsService } from '../../services/posts.service';
import { ToastService } from '../../services/toast.service';
import { ReportModalComponent } from '../report-modal/report-modal.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-post-dropdown',
  standalone: true,
  imports: [CommonModule, ReportModalComponent],
  templateUrl: './post-dropdown.component.html',
  styleUrl: './post-dropdown.component.css'
})
export class PostDropdownComponent implements OnInit, OnDestroy {
  @Input() post!: Post;
  @Input() currentUserId!: string;
  @Input() isOpen = false;
  
  @Output() editPost = new EventEmitter<Post>();
  @Output() deletePost = new EventEmitter<Post>();
  @Output() toggleDropdown = new EventEmitter<void>();
  @Output() followStatusChanged = new EventEmitter<void>();

  isFollowing = false;
  isLoadingFollowStatus = false;
  isLoadingFollowAction = false;
  isReporting = false;
  showReportModal = false;
  private followStatusSubscription?: Subscription;

  constructor(
    private followsService: FollowsService,
    private postsService: PostsService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.checkFollowStatus();
  }

  ngOnDestroy(): void {
    if (this.followStatusSubscription) {
      this.followStatusSubscription.unsubscribe();
    }
  }

  get canEditOrDelete(): boolean {
    return this.post.userId === this.currentUserId;
  }

  get canFollow(): boolean {
    const authorId = this.post.author?.userId || this.post.userId;
    return authorId !== this.currentUserId && authorId !== '';
  }

  get canReport(): boolean {
    // Users cannot report their own posts
    // Show report button for any post that is not owned by the current user
    if (!this.currentUserId || !this.post) {
      return false;
    }
    const authorId = this.post.author?.userId || this.post.userId;
    const isOwnPost = authorId === this.currentUserId;
    return !isOwnPost && !!authorId;
  }

  checkFollowStatus(): void {
    if (!this.canFollow) {
      return;
    }

    const authorId = this.post.author?.userId || this.post.userId;
    if (!authorId) {
      return;
    }

    this.isLoadingFollowStatus = true;
    this.followStatusSubscription = this.followsService.getFollowStatus(authorId).subscribe({
      next: (status) => {
        this.isFollowing = status.isFollowing;
        this.isLoadingFollowStatus = false;
      },
      error: (error) => {
        console.error('Error checking follow status:', error);
        this.isFollowing = false;
        this.isLoadingFollowStatus = false;
      }
    });
  }

  onEdit(): void {
    this.editPost.emit(this.post);
    this.toggleDropdown.emit();
  }

  onDelete(): void {
    this.deletePost.emit(this.post);
    this.toggleDropdown.emit();
  }

  onToggleDropdown(event: Event): void {
    event.stopPropagation();
    this.toggleDropdown.emit();
  }

  onFollowToggle(): void {
    if (!this.canFollow || this.isLoadingFollowAction) {
      return;
    }

    const authorId = this.post.author?.userId || this.post.userId;
    if (!authorId) {
      return;
    }

    this.isLoadingFollowAction = true;
    const action = this.isFollowing 
      ? this.followsService.unfollowUser(authorId)
      : this.followsService.followUser(authorId);

    action.subscribe({
      next: () => {
        this.isFollowing = !this.isFollowing;
        this.isLoadingFollowAction = false;
        this.toggleDropdown.emit();
        this.followStatusChanged.emit();
      },
      error: (error) => {
        console.error('Error following/unfollowing user:', error);
        this.isLoadingFollowAction = false;
        alert('Failed to ' + (this.isFollowing ? 'unfollow' : 'follow') + ' user. Please try again.');
      }
    });
  }

  onReport(): void {
    if (!this.canReport || this.isReporting) {
      return;
    }
    this.showReportModal = true;
    this.toggleDropdown.emit();
  }

  onReportConfirm(reason?: string): void {
    this.showReportModal = false;
    this.isReporting = true;

    this.postsService.reportPost(this.post.postId, reason).subscribe({
      next: () => {
        this.toastService.success('Post Reported', 'The post has been reported successfully.');
        this.isReporting = false;
      },
      error: (error) => {
        console.error('Error reporting post:', error);
        const errorMessage = error.error?.message || 'Failed to report post. Please try again.';
        this.toastService.error('Report Failed', errorMessage);
        this.isReporting = false;
      }
    });
  }

  onReportCancel(): void {
    this.showReportModal = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdownElement = target.closest('.post-dropdown');
    
    if (!dropdownElement && this.isOpen) {
      this.toggleDropdown.emit();
    }
  }
}
