import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService, User } from '../../services/user.service';
import { PostsService } from '../../services/posts.service';
import { Post } from '../../interfaces';
import { FollowsService } from '../../services/follows.service';
import { AuthService } from '../../services/auth.service';
import { NavbarDropdownComponent } from '../navbar-dropdown/navbar-dropdown.component';
import { PostDropdownComponent } from '../post-dropdown/post-dropdown.component';
import { PostCommentsComponent } from '../post-comments/post-comments.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { ToastService } from '../../services/toast.service';
import { LikesService } from '../../services/likes.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    NavbarDropdownComponent,
    PostDropdownComponent,
    PostCommentsComponent,
    ConfirmationModalComponent
  ],
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit {
  userId: string = '';
  user: User | null = null;
  posts: Post[] = [];
  isLoading = false;
  isLoadingPosts = false;
  isFollowing = false;
  isLoadingFollowStatus = false;
  currentUser: any = null;
  
  // Post interaction states
  openDropdowns: { [postId: string]: boolean } = {};
  showComments: { [postId: string]: boolean } = {};
  showDeleteModal = false;
  postToDelete: Post | null = null;
  editingPost: Post | null = null;
  editPostForm = {
    content: '',
    imageUrl: ''
  };
  isSavingPost = false;
  isDeletingPost = false;

  // Translation and summarization states
  translatingPosts: { [postId: string]: boolean } = {};
  summarizingPosts: { [postId: string]: boolean } = {};
  translationLanguage: string = 'en';
  showLanguageSelector: { [postId: string]: boolean } = {};
  
  availableLanguages = [
    { code: 'en', name: 'English' },
    { code: 'ar', name: 'Arabic' },
    { code: 'fr', name: 'French' },
    { code: 'es', name: 'Spanish' },
    { code: 'de', name: 'German' },
    { code: 'it', name: 'Italian' },
    { code: 'pt', name: 'Portuguese' },
    { code: 'ru', name: 'Russian' },
    { code: 'ja', name: 'Japanese' },
    { code: 'ko', name: 'Korean' },
    { code: 'zh', name: 'Chinese' },
    { code: 'hi', name: 'Hindi' },
    { code: 'tr', name: 'Turkish' },
    { code: 'pl', name: 'Polish' },
    { code: 'nl', name: 'Dutch' },
    { code: 'sv', name: 'Swedish' },
    { code: 'da', name: 'Danish' },
    { code: 'no', name: 'Norwegian' },
    { code: 'fi', name: 'Finnish' },
    { code: 'cs', name: 'Czech' }
  ];

  // Profile photo cache
  profilePhotoCache: { [userId: string]: string } = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private postsService: PostsService,
    private followsService: FollowsService,
    private authService: AuthService,
    private toastService: ToastService,
    private likesService: LikesService
  ) {}

  ngOnInit(): void {
    // Get current user
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    // Get userId from route params
    this.route.params.subscribe(params => {
      this.userId = params['userId'];
      if (this.userId) {
        this.loadUserProfile();
        this.loadUserPosts();
        if (this.currentUser?.id) {
          this.checkFollowStatus();
        }
      }
    });
  }

  loadUserProfile(): void {
    this.isLoading = true;
    this.userService.getUserById(this.userId).subscribe({
      next: (user) => {
        this.user = user;
        this.isLoading = false;
        // Load profile photo if available
        if (user.profilePhotoId) {
          this.loadProfilePhotoUrl(user.profilePhotoId);
        }
      },
      error: (error) => {
        console.error('Error loading user profile:', error);
        this.isLoading = false;
        this.toastService.error('Error', 'Failed to load user profile.');
        this.router.navigate(['/home']);
      }
    });
  }

  loadUserPosts(): void {
    this.isLoadingPosts = true;
    this.postsService.getPostsByUser(this.userId).subscribe({
      next: (posts) => {
        this.posts = posts.map(post => this.transformPostForDisplay(post));
        this.isLoadingPosts = false;
        
        // Load profile photos for all post authors
        posts.forEach(post => {
          if (post.author?.userId && !this.profilePhotoCache[post.author.userId]) {
            // Profile photo loading would be handled if needed
          }
        });
      },
      error: (error) => {
        console.error('Error loading user posts:', error);
        this.isLoadingPosts = false;
        this.toastService.error('Error', 'Failed to load user posts.');
      }
    });
  }

  checkFollowStatus(): void {
    if (!this.currentUser?.id || this.currentUser.id === this.userId) {
      return; // Don't check if viewing own profile
    }
    
    this.isLoadingFollowStatus = true;
    this.followsService.getFollowStatus(this.userId).subscribe({
      next: (response) => {
        this.isFollowing = response.isFollowing;
        this.isLoadingFollowStatus = false;
      },
      error: (error) => {
        console.error('Error checking follow status:', error);
        this.isFollowing = false;
        this.isLoadingFollowStatus = false;
      }
    });
  }

  onFollowToggle(): void {
    if (!this.currentUser?.id) {
      this.toastService.error('Error', 'Please log in to follow users.');
      return;
    }

    if (this.currentUser.id === this.userId) {
      return; // Can't follow yourself
    }

    this.isLoadingFollowStatus = true;
    const action = this.isFollowing
      ? this.followsService.unfollowUser(this.userId)
      : this.followsService.followUser(this.userId);

    action.subscribe({
      next: () => {
        this.isFollowing = !this.isFollowing;
        this.isLoadingFollowStatus = false;
        this.toastService.success(
          this.isFollowing ? 'Following' : 'Unfollowed',
          this.isFollowing ? `You are now following ${this.user?.name}` : `You unfollowed ${this.user?.name}`
        );
      },
      error: (error) => {
        console.error('Error toggling follow:', error);
        this.isLoadingFollowStatus = false;
        this.toastService.error('Error', 'Failed to update follow status.');
      }
    });
  }

  loadProfilePhotoUrl(imageId: string): void {
    if (!imageId || this.profilePhotoCache[imageId]) {
      return;
    }

    const url = `${environment.apiUrl}/images/${imageId}/url`;
    fetch(url, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      }
    })
      .then(response => response.json())
      .then(data => {
        if (data && data.url) {
          this.profilePhotoCache[imageId] = data.url;
          if (this.user) {
            (this.user as any).profilePhotoUrl = data.url;
          }
        }
      })
      .catch(error => {
        console.error('Error loading profile photo:', error);
      });
  }

  transformPostForDisplay(post: Post): Post {
    // Transform post similar to home component
    if (post.authorName && !post.author) {
      post.author = {
        name: post.authorName,
        initials: this.getInitials(post.authorName),
        userId: post.userId,
        profilePhotoUrl: this.profilePhotoCache[post.userId] || null
      };
    }
    return post;
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    const words = name.trim().split(' ');
    if (words.length === 1) {
      return words[0].substring(0, 2).toUpperCase();
    }
    return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
  }

  getUserProfilePhotoUrl(): string | null {
    if (!this.user) return null;
    return (this.user as any).profilePhotoUrl || this.profilePhotoCache[this.user.profilePhotoId || ''] || null;
  }

  togglePostDropdown(postId: string): void {
    this.openDropdowns[postId] = !this.openDropdowns[postId];
  }

  isDropdownOpen(postId: string): boolean {
    return this.openDropdowns[postId] || false;
  }

  toggleComments(postId: string): void {
    this.showComments[postId] = !this.showComments[postId];
  }

  onEditPost(post: Post): void {
    this.editingPost = post;
    this.editPostForm.content = post.content;
    this.editPostForm.imageUrl = post.imageUrl || '';
  }

  onSaveEdit(): void {
    if (!this.editingPost) return;

    this.isSavingPost = true;
    const updateRequest = {
      content: this.editPostForm.content.trim(),
      imageUrl: this.editPostForm.imageUrl.trim() || undefined
    };

    this.postsService.updatePost(this.editingPost.postId, updateRequest, this.currentUser.id).subscribe({
      next: (updatedPost) => {
        const index = this.posts.findIndex(p => p.postId === updatedPost.postId);
        if (index !== -1) {
          this.posts[index] = this.transformPostForDisplay(updatedPost);
        }
        this.editingPost = null;
        this.editPostForm = { content: '', imageUrl: '' };
        this.isSavingPost = false;
        this.toastService.success('Success', 'Post updated successfully.');
      },
      error: (error) => {
        console.error('Error updating post:', error);
        this.isSavingPost = false;
        this.toastService.error('Error', 'Failed to update post.');
      }
    });
  }

  onDeletePost(post: Post): void {
    this.postToDelete = post;
    this.showDeleteModal = true;
  }

  onConfirmDelete(): void {
    if (!this.postToDelete) return;

    this.isDeletingPost = true;
    this.postsService.deletePost(this.postToDelete.postId, this.currentUser.id).subscribe({
      next: () => {
        this.posts = this.posts.filter(p => p.postId !== this.postToDelete!.postId);
        this.showDeleteModal = false;
        this.postToDelete = null;
        this.isDeletingPost = false;
        this.toastService.success('Success', 'Post deleted successfully.');
      },
      error: (error) => {
        console.error('Error deleting post:', error);
        this.isDeletingPost = false;
        this.toastService.error('Error', 'Failed to delete post.');
      }
    });
  }

  onCancelDelete(): void {
    this.showDeleteModal = false;
    this.postToDelete = null;
  }

  likePost(postId: string): void {
    const post = this.posts.find(p => p.postId === postId);
    if (!post) return;

    const wasLiked = post.isLikedByCurrentUser;
    post.isLikedByCurrentUser = !wasLiked;
    post.likesCount += post.isLikedByCurrentUser ? 1 : -1;

    this.likesService.toggleLike(postId).subscribe({
      next: (response) => {
        post.isLikedByCurrentUser = response.isLiked ?? post.isLikedByCurrentUser;
        post.likesCount = response.likesCount;
      },
      error: (error) => {
        console.error('Error toggling like:', error);
        post.isLikedByCurrentUser = wasLiked;
        post.likesCount += post.isLikedByCurrentUser ? 1 : -1;
        this.toastService.error('Error', 'Failed to update like.');
      }
    });
  }

  onCommentsCountChanged(postId: string, newCount: number): void {
    const post = this.posts.find(p => p.postId === postId);
    if (post) {
      post.commentsCount = newCount;
    }
  }

  translatePost(postId: string, language?: string): void {
    if (this.translatingPosts[postId]) return;

    const post = this.posts.find(p => p.postId === postId);
    if (!post) return;

    if (post.isTranslated && post.translatedContent && !language) {
      post.isTranslated = false;
      post.translatedContent = undefined;
      post.translationLanguage = undefined;
      this.showLanguageSelector[postId] = false;
      return;
    }

    const targetLanguage = language || this.translationLanguage;
    this.showLanguageSelector[postId] = false;
    this.translatingPosts[postId] = true;

    this.postsService.translatePost(postId, targetLanguage).subscribe({
      next: (response) => {
        post.translatedContent = response.translated;
        post.isTranslated = true;
        post.translationLanguage = response.language;
        this.translatingPosts[postId] = false;
        this.toastService.success('Translated', `Post has been translated to ${this.getLanguageName(response.language)}.`);
      },
      error: (error) => {
        console.error('Error translating post:', error);
        this.translatingPosts[postId] = false;
        this.toastService.error('Translation Failed', 'Failed to translate post. Please try again.');
      }
    });
  }

  summarizePost(postId: string): void {
    if (this.summarizingPosts[postId]) return;

    const post = this.posts.find(p => p.postId === postId);
    if (!post) return;

    if (post.isSummarized && post.summarizedContent) {
      post.isSummarized = false;
      post.summarizedContent = undefined;
      if (post.isTranslated) {
        post.isTranslated = false;
        post.translatedContent = undefined;
        post.translationLanguage = undefined;
      }
      return;
    }

    this.summarizingPosts[postId] = true;

    this.postsService.summarizePost(postId).subscribe({
      next: (response) => {
        post.summarizedContent = response.summary;
        post.isSummarized = true;
        this.summarizingPosts[postId] = false;
        this.toastService.success('Summarized', 'Post has been summarized successfully.');
      },
      error: (error) => {
        console.error('Error summarizing post:', error);
        this.summarizingPosts[postId] = false;
        this.toastService.error('Summarization Failed', 'Failed to summarize post. Please try again.');
      }
    });
  }

  toggleLanguageSelector(postId: string, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.showLanguageSelector[postId] = !this.showLanguageSelector[postId];
  }

  closeLanguageSelector(postId: string): void {
    this.showLanguageSelector[postId] = false;
  }

  getLanguageName(code: string): string {
    const lang = this.availableLanguages.find(l => l.code === code);
    return lang ? lang.name : code.toUpperCase();
  }

  get isOwnProfile(): boolean {
    return this.currentUser?.id === this.userId;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (diffInSeconds < 60) {
      return 'just now';
    } else if (diffInSeconds < 3600) {
      const minutes = Math.floor(diffInSeconds / 60);
      return `${minutes} minute${minutes > 1 ? 's' : ''} ago`;
    } else if (diffInSeconds < 86400) {
      const hours = Math.floor(diffInSeconds / 3600);
      return `${hours} hour${hours > 1 ? 's' : ''} ago`;
    } else if (diffInSeconds < 604800) {
      const days = Math.floor(diffInSeconds / 86400);
      return `${days} day${days > 1 ? 's' : ''} ago`;
    } else {
      return date.toLocaleDateString();
    }
  }

  onFollowStatusChanged(): void {
    // Refresh follow status if needed
    if (this.currentUser?.id) {
      this.checkFollowStatus();
    }
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}

