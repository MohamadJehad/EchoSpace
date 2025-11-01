import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommentsService } from '../../services/comments.service';
import { ToastService } from '../../services/toast.service';
import { Comment, CreateCommentRequest } from '../../interfaces/comment.interface';

@Component({
  selector: 'app-post-comments',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
  @Input() isOpen: boolean = false;

  @Output() commentsCountChanged = new EventEmitter<number>();
  @Output() toggleRequested = new EventEmitter<void>();

  comments: Comment[] = [];
  isLoadingComments = false;
  newCommentContent: string = '';
  isCreatingComment = false;
  editingCommentId: string | null = null;
  editCommentContent: string = '';

  constructor(
    private commentsService: CommentsService,
    private toastService: ToastService
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
          timeAgo: this.calculateTimeAgo(c.createdAt)
        }));
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
          timeAgo: 'just now'
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
        this.toastService.error('Error', 'Failed to add comment. Please try again.');
      }
    });
  }

  deleteComment(commentId: string): void {
    if (!confirm('Are you sure you want to delete this comment?')) {
      return;
    }

    this.commentsService.deleteComment(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.commentId !== commentId);

        // Update comment count
        if (this.commentsCount > 0) {
          this.commentsCount--;
          this.commentsCountChanged.emit(this.commentsCount);
        }

        this.toastService.success('Success!', 'Comment deleted successfully.');
      },
      error: (error) => {
        console.error('Error deleting comment:', error);
        this.toastService.error('Error', 'Failed to delete comment. Please try again.');
      }
    });
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
            timeAgo: this.calculateTimeAgo(updatedComment.createdAt)
          };
        }

        this.editingCommentId = null;
        this.editCommentContent = '';
        this.toastService.success('Success!', 'Comment updated successfully.');
      },
      error: (error) => {
        console.error('Error updating comment:', error);
        this.toastService.error('Error', 'Failed to update comment. Please try again.');
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

