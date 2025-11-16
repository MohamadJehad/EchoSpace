import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommentsService } from '../../services/comments.service';
import { ToastService } from '../../services/toast.service';
import { Comment, CreateCommentRequest } from '../../interfaces/comment.interface';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-post-comments',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmationModalComponent],
  templateUrl: './post-comments.component.html',
  styleUrl: './post-comments.component.css'
})
export class PostCommentsComponent implements OnInit, OnChanges {
  @Input() postId!: string;
  @Input() commentsCount: number = 0;
  @Input() currentUserId: string = '';
  @Input() currentUserInitials: string = '';
  @Input() currentUserName: string = '';
  @Input() currentUserEmail: string = '';
  @Input() currentUserProfilePhotoUrl: string | null = null;
  @Input() isOpen: boolean = false;
  @Input() allowCreate: boolean = true; // Allow/disallow creating new comments

  @Output() commentsCountChanged = new EventEmitter<number>();
  @Output() toggleRequested = new EventEmitter<void>();

  comments: Comment[] = [];
  isLoadingComments = false;
  newCommentContent: string = '';
  isCreatingComment = false;
  editingCommentId: string | null = null;
  editCommentContent: string = '';
  
  // Delete confirmation modal state
  showDeleteModal = false;
  commentIdToDelete: string | null = null;
  isDeletingComment = false;

  // Cache for profile photos by user ID
  profilePhotoCache: { [userId: string]: string } = {};

  constructor(
    private commentsService: CommentsService,
    private toastService: ToastService,
    private userService: UserService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    if (this.isOpen) {
      this.loadComments();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Load comments when component is opened
    if (changes['isOpen'] && changes['isOpen'].currentValue === true && !changes['isOpen'].previousValue) {
      if (this.comments.length === 0) {
        this.loadComments();
      }
    }
  }

  toggleComments(): void {
    this.toggleRequested.emit();
    if (!this.isOpen && this.comments.length === 0) {
      this.loadComments();
    }
  }

  loadComments(): void {
    this.isLoadingComments = true;
    
    this.commentsService.getCommentsByPost(this.postId).subscribe({
      next: (comments) => {
        this.comments = comments.map(c => ({
          ...c,
          timeAgo: this.calculateTimeAgo(c.createdAt),
          // Check cache or set profile photo if current user
          profilePhotoUrl: c.userId === this.currentUserId 
            ? this.currentUserProfilePhotoUrl 
            : this.profilePhotoCache[c.userId] || null
        }));
        
        // Load profile photos for all comment authors
        this.loadProfilePhotosForComments(this.comments);
        this.isLoadingComments = false;
      },
      error: (error) => {
        console.error('Error loading comments:', error);
        this.isLoadingComments = false;
        this.toastService.error('Error', 'Failed to load comments. Please try again.');
      }
    });
  }

  createComment(): void {
    const content = this.newCommentContent.trim();
    if (!content) {
      return;
    }

    if (!this.currentUserId) {
      this.toastService.error('Error', 'User not authenticated. Please log in again.');
      return;
    }

    this.isCreatingComment = true;

    const request: CreateCommentRequest = {
      postId: this.postId,
      userId: this.currentUserId,
      content: content
    };

    this.commentsService.createComment(request).subscribe({
      next: (newComment) => {
        // Ensure user info is populated - use current user info if backend doesn't return it
        const commentWithUserInfo: Comment = {
          ...newComment,
          userName: newComment.userName || this.currentUserName,
          userEmail: newComment.userEmail || this.currentUserEmail,
          timeAgo: 'just now',
          profilePhotoUrl: this.currentUserProfilePhotoUrl
        };
        
        this.comments.unshift(commentWithUserInfo);

        this.newCommentContent = '';
        this.isCreatingComment = false;
        
        // Update comment count
        this.commentsCount++;
        this.commentsCountChanged.emit(this.commentsCount);
        
        this.toastService.success('Success!', 'Your comment has been added.');
      },
      error: (error) => {
        console.error('Error creating comment:', error);
        this.isCreatingComment = false;
        this.toastService.error('Error', 'Failed to add comment.Comment contains toxic or unsafe contenets. Please edit it and try again.');
      }
    });
  }

  deleteComment(commentId: string): void {
    this.commentIdToDelete = commentId;
    this.showDeleteModal = true;
  }

  onConfirmDelete(): void {
    if (!this.commentIdToDelete) {
      return;
    }

    this.isDeletingComment = true;

    this.commentsService.deleteComment(this.commentIdToDelete).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.commentId !== this.commentIdToDelete);

        // Update comment count
        if (this.commentsCount > 0) {
          this.commentsCount--;
          this.commentsCountChanged.emit(this.commentsCount);
        }

        this.isDeletingComment = false;
        this.showDeleteModal = false;
        this.commentIdToDelete = null;

        this.toastService.success('Success!', 'Comment deleted successfully.');
      },
      error: (error) => {
        console.error('Error deleting comment:', error);
        this.isDeletingComment = false;
        this.toastService.error('Error', 'Failed to delete comment. Please try again.');
      }
    });
  }

  onCancelDelete(): void {
    this.showDeleteModal = false;
    this.commentIdToDelete = null;
  }

  startEditComment(comment: Comment): void {
    this.editingCommentId = comment.commentId;
    this.editCommentContent = comment.content;
  }

  cancelEditComment(): void {
    this.editingCommentId = null;
    this.editCommentContent = '';
  }

  saveEditComment(commentId: string): void {
    if (!this.editCommentContent.trim()) {
      return;
    }

    this.commentsService.updateComment(commentId, { content: this.editCommentContent.trim() }).subscribe({
      next: (updatedComment) => {
        const index = this.comments.findIndex(c => c.commentId === commentId);
        if (index !== -1) {
          this.comments[index] = {
            ...updatedComment,
            timeAgo: this.calculateTimeAgo(updatedComment.createdAt),
            // Preserve existing profile photo URL
            profilePhotoUrl: this.comments[index].profilePhotoUrl
          };
        }

        this.editingCommentId = null;
        this.editCommentContent = '';
        this.toastService.success('Success!', 'Comment updated successfully.');
      },
      error: (error) => {
        console.error('Error updating comment:', error);
        this.toastService.error('Error', 'Failed to update comment.Comment contains toxic or unsafe contenets. Please edit it and try again.');
      }
    });
  }

  isCommentOwner(comment: Comment): boolean {
    return comment.userId === this.currentUserId;
  }

  getCommentInitials(userName: string, userEmail: string): string {
    if (userName) {
      const names = userName.split(' ');
      if (names.length >= 2) {
        return (names[0][0] + names[names.length - 1][0]).toUpperCase();
      }
      return userName.substring(0, 2).toUpperCase();
    }
    if (userEmail) {
      return userEmail.substring(0, 2).toUpperCase();
    }
    return 'U';
  }

  private loadProfilePhotosForComments(comments: Comment[]): void {
    // Get unique user IDs from comments (excluding current user)
    const userIds = [...new Set(
      comments
        .map(comment => comment.userId)
        .filter(id => id && id !== this.currentUserId && !this.profilePhotoCache[id])
    )];
    
    // Load profile photos for each unique user
    userIds.forEach(userId => {
      if (userId) {
        this.loadAuthorProfilePhoto(userId);
      }
    });
  }

  private loadAuthorProfilePhoto(userId: string): void {
    this.userService.getUserById(userId).subscribe({
      next: (user: any) => {
        // Handle both camelCase and PascalCase property names
        const profilePhotoId = user.profilePhotoId || user.ProfilePhotoId;
        if (profilePhotoId) {
          this.loadProfilePhotoUrlForAuthor(userId, profilePhotoId);
        }
      },
      error: (error) => {
        console.error(`Error loading user ${userId}:`, error);
      }
    });
  }

  private loadProfilePhotoUrlForAuthor(userId: string, imageId: string): void {
    // environment.apiUrl already includes /api, so we don't need to add it again
    fetch(`${environment.apiUrl}/images/${imageId}/url`, {
      headers: {
        'Authorization': `Bearer ${this.authService.getToken()}`
      }
    })
    .then(response => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then(data => {
      if (data && data.url) {
        // Cache the profile photo URL
        this.profilePhotoCache[userId] = data.url;
        
        // Update all comments with this author's profile photo
        this.comments.forEach(comment => {
          if (comment.userId === userId) {
            comment.profilePhotoUrl = data.url;
          }
        });
      }
    })
    .catch(error => {
      console.error(`Error loading profile photo URL for user ${userId}:`, error);
    });
  }

  private calculateTimeAgo(createdAt: string): string {
    const now = new Date();
    const commentDate = new Date(createdAt);
    const diffInSeconds = Math.floor((now.getTime() - commentDate.getTime()) / 1000);

    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
  }
}

