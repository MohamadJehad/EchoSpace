import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService, User } from '../../services/user.service';
import { PostsService } from '../../services/posts.service';
import { Post, ReportedPost } from '../../interfaces/post.interface';
import { NavbarComponent } from '../navbar/navbar.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { PostCommentsComponent } from '../post-comments/post-comments.component';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-operation-home',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, NavbarComponent, ConfirmationModalComponent, PostCommentsComponent],
  templateUrl: './operation-home.component.html',
  styleUrls: ['./operation-home.component.css']
})
export class OperationHomeComponent implements OnInit {
  users: User[] = [];
  posts: Post[] = [];
  reportedPosts: ReportedPost[] = [];
  loading = false;
  usersLoading = false;
  postsLoading = false;
  reportedPostsLoading = false;
  error: string | null = null;
  
  // Modal states
  showUserModal = false;
  showPostModal = false;
  pendingUserId: string | null = null;
  pendingPostId: string | null = null;
  pendingAction: 'block' | 'unblock' | 'delete' | null = null;
  isProcessing = false;

  // Active tab
  activeTab: 'posts' | 'reported' | 'users' = 'reported';

  // Search/filter
  searchQuery = '';
  filteredPosts: Post[] = [];
  filteredUsers: User[] = [];

  // Comments
  showComments: { [postId: string]: boolean } = {};
  
  // Report details
  showReportDetails: { [postId: string]: boolean } = {};

  currentUser = {
    name: '',
    email: '',
    role: '',
    id: '',
    profilePhotoUrl: null as string | null
  };

  constructor(
    private userService: UserService,
    private postsService: PostsService,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadReportedPosts();
    this.loadPosts();
    this.loadUsers();
  }

  loadCurrentUser(): void {
    // Get user from authService instead of directly from cookies
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUser = {
          name: user.name || '',
          email: user.email || '',
          role: user.role || '',
          id: user.id || '',
          profilePhotoUrl: user.profilePhotoUrl || null
        };
      }
    });
  }

  loadReportedPosts(): void {
    this.reportedPostsLoading = true;
    this.error = null;
    
    this.postsService.getReportedPosts().subscribe({
      next: (data) => {
        console.log('Reported posts data:', data);
        this.reportedPosts = data;
        // Ensure reports array exists for each post
        this.reportedPosts.forEach(post => {
          if (!post.reports) {
            post.reports = [];
          }
          console.log(`Post ${post.postId} has ${post.reports.length} reports:`, post.reports);
        });
        this.reportedPostsLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load reported posts';
        this.reportedPostsLoading = false;
        console.error('Error loading reported posts:', err);
      }
    });
  }

  loadPosts(): void {
    this.postsLoading = true;
    this.error = null;
    
    // Use getAllPosts with proper headers (it should include auth token)
    this.postsService.getAllPosts().subscribe({
      next: (data) => {
        this.posts = data;
        this.filteredPosts = data;
        this.postsLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load posts';
        this.postsLoading = false;
        console.error(err);
      }
    });
  }

  loadUsers(): void {
    this.usersLoading = true;
    this.error = null;
    
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.filteredUsers = data;
        this.usersLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load users';
        this.usersLoading = false;
        console.error(err);
      }
    });
  }

  onSearchChange(): void {
    if (this.activeTab === 'posts') {
      if (!this.searchQuery.trim()) {
        this.filteredPosts = this.posts;
      } else {
        const query = this.searchQuery.toLowerCase();
        this.filteredPosts = this.posts.filter(post => 
          post.content?.toLowerCase().includes(query) ||
          post.author?.name?.toLowerCase().includes(query) ||
          post.authorName?.toLowerCase().includes(query)
        );
      }
    } else {
      if (!this.searchQuery.trim()) {
        this.filteredUsers = this.users;
      } else {
        const query = this.searchQuery.toLowerCase();
        this.filteredUsers = this.users.filter(user => 
          user.name?.toLowerCase().includes(query) ||
          user.email?.toLowerCase().includes(query)
        );
      }
    }
  }

  blockUser(id: string): void {
    this.pendingUserId = id;
    this.pendingAction = 'block';
    this.showUserModal = true;
  }

  unblockUser(id: string): void {
    this.pendingUserId = id;
    this.pendingAction = 'unblock';
    this.showUserModal = true;
  }

  deletePost(id: string): void {
    this.pendingPostId = id;
    this.pendingAction = 'delete';
    this.showPostModal = true;
  }

  onConfirmUserAction(): void {
    if (!this.pendingUserId || !this.pendingAction) return;

    this.isProcessing = true;
    const userId = this.pendingUserId;
    const action = this.pendingAction;

    const handleSuccess = (updatedUser?: User) => {
      this.error = null;
      if (updatedUser) {
        const index = this.users.findIndex(u => u.id === updatedUser.id);
        if (index !== -1) {
          this.users[index] = { ...updatedUser };
        }
        const filterIndex = this.filteredUsers.findIndex(u => u.id === updatedUser.id);
        if (filterIndex !== -1) {
          this.filteredUsers[filterIndex] = { ...updatedUser };
        }
      }
      this.loadUsers();
      this.isProcessing = false;
      this.showUserModal = false;
      this.pendingUserId = null;
      this.pendingAction = null;
    };

    const handleError = (err: unknown) => {
      this.isProcessing = false;
      console.error(`Failed to ${action} user`, err);
      const errorMessage = err && typeof err === 'object' && 'error' in err && err.error && typeof err.error === 'object' && 'message' in err.error
        ? String(err.error.message)
        : err && typeof err === 'object' && 'message' in err
        ? String(err.message)
        : 'Unknown error';
      this.error = `Failed to ${action} user: ${errorMessage}`;
      this.showUserModal = false;
      this.pendingUserId = null;
      this.pendingAction = null;
    };

    if (action === 'block') {
      this.userService.lockUser(userId).subscribe({ 
        next: (user) => handleSuccess(user), 
        error: handleError 
      });
    } else if (action === 'unblock') {
      this.userService.unlockUser(userId).subscribe({ 
        next: (user) => handleSuccess(user), 
        error: handleError 
      });
    }
  }

  onConfirmPostAction(): void {
    if (!this.pendingPostId || this.pendingAction !== 'delete') return;

    this.isProcessing = true;
    const postId = this.pendingPostId;

    this.postsService.deletePost(postId, this.currentUser.id).subscribe({
      next: () => {
        this.error = null;
        // Remove from regular posts
        this.posts = this.posts.filter(p => p.postId !== postId);
        this.filteredPosts = this.filteredPosts.filter(p => p.postId !== postId);
        // Remove from reported posts
        this.reportedPosts = this.reportedPosts.filter(p => p.postId !== postId);
        this.isProcessing = false;
        this.showPostModal = false;
        this.pendingPostId = null;
        this.pendingAction = null;
      },
      error: (err) => {
        this.isProcessing = false;
        console.error('Failed to delete post', err);
        this.error = `Failed to delete post: ${err.error?.message || err.message || 'Unknown error'}`;
        this.showPostModal = false;
        this.pendingPostId = null;
        this.pendingAction = null;
      }
    });
  }

  onCancelUserModal(): void {
    this.showUserModal = false;
    this.pendingUserId = null;
    this.pendingAction = null;
    this.isProcessing = false;
  }

  onCancelPostModal(): void {
    this.showPostModal = false;
    this.pendingPostId = null;
    this.pendingAction = null;
    this.isProcessing = false;
  }

  getUserModalConfig() {
    switch (this.pendingAction) {
      case 'block':
        return {
          title: 'Block User Account',
          message: 'Are you sure you want to block this user account? The user will not be able to log in until the account is unblocked.',
          confirmText: 'Block Account',
          buttonClass: 'bg-amber-600 hover:bg-amber-700'
        };
      case 'unblock':
        return {
          title: 'Unblock User Account',
          message: 'Are you sure you want to unblock this user account? The user will be able to log in again.',
          confirmText: 'Unblock Account',
          buttonClass: 'bg-emerald-600 hover:bg-emerald-700'
        };
      default:
        return {
          title: 'Confirm Action',
          message: 'Are you sure?',
          confirmText: 'Confirm',
          buttonClass: 'bg-amber-600 hover:bg-amber-700'
        };
    }
  }

  getPostModalConfig() {
    return {
      title: 'Delete Post',
      message: 'Are you sure you want to delete this post? This action cannot be undone.',
      confirmText: 'Delete Post',
      buttonClass: 'bg-red-600 hover:bg-red-700'
    };
  }

  isAccountBlocked(user: User): boolean {
    if (!user.lockoutEnabled || !user.lockoutEnd) {
      return false;
    }
    const lockoutEnd = new Date(user.lockoutEnd);
    return lockoutEnd > new Date();
  }

  getBlockStatus(user: User): string {
    if (!user.lockoutEnabled) {
      return 'Active';
    }
    if (!user.lockoutEnd) {
      return 'Active';
    }
    const lockoutEnd = new Date(user.lockoutEnd);
    if (lockoutEnd > new Date()) {
      return `Blocked until ${lockoutEnd.toLocaleString()}`;
    }
    return 'Active';
  }

  switchTab(tab: 'posts' | 'reported' | 'users'): void {
    this.activeTab = tab;
    this.searchQuery = '';
    if (tab === 'posts') {
      this.filteredPosts = this.posts;
    } else if (tab === 'reported') {
      this.loadReportedPosts();
    } else {
      this.filteredUsers = this.users;
    }
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return 'Unknown';
    return new Date(dateString).toLocaleString();
  }

  toggleComments(postId: string): void {
    this.showComments[postId] = !this.showComments[postId];
    if (this.showComments[postId]) {
      // Comments will be loaded by the PostCommentsComponent
    }
  }

  onCommentsCountChanged(postId: string, newCount: number): void {
    const post = this.posts.find(p => p.postId === postId);
    if (post) {
      post.commentsCount = newCount;
    }
    const filteredPost = this.filteredPosts.find(p => p.postId === postId);
    if (filteredPost) {
      filteredPost.commentsCount = newCount;
    }
  }

  toggleReportDetails(postId: string): void {
    this.showReportDetails[postId] = !this.showReportDetails[postId];
    const post = this.reportedPosts.find(p => p.postId === postId);
    console.log(`Toggling report details for post ${postId}:`, {
      isOpen: this.showReportDetails[postId],
      hasReports: post?.reports ? post.reports.length : 0,
      reports: post?.reports
    });
  }
}

