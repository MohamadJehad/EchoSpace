import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PostsService } from '../../services/posts.service';
import { FollowsService } from '../../services/follows.service';
import { LikesService } from '../../services/likes.service';
import { NavbarDropdownComponent } from '../navbar-dropdown/navbar-dropdown.component';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { SuggestedUsersComponent } from '../suggested-users/suggested-users.component';
import { PostDropdownComponent } from '../post-dropdown/post-dropdown.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { ToastService } from '../../services/toast.service';
import { Post, TrendingTopic, CreatePostRequest, UpdatePostRequest } from '../../interfaces';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, NavbarDropdownComponent, SearchBarComponent, SuggestedUsersComponent, PostDropdownComponent, ConfirmationModalComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  isLoading = false;
  isCreatingPost = false;
  posts: Post[] = []; // Remove mock data
  
  // Create post form
  newPost = {
    content: '',
    imageUrl: ''
  };

  // Photo upload
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  
  // Post editing
  editingPost: Post | null = null;
  editPostForm = {
    content: '',
    imageUrl: ''
  };
  isEditingPost = false;
  isSavingPost = false;
  isDeletingPost = false;
  
  // Dropdown states
  openDropdowns: { [postId: string]: boolean } = {};
  
  // Confirmation modal
  showDeleteModal = false;
  postToDelete: Post | null = null;
  
  currentUser = {
    name: 'John Doe',
    email: 'john.doe@example.com',
    initials: 'JD',
    role: 'User',
    id: ''
  };

  // User statistics
  userStats = {
    postsCount: 0,
    followersCount: 0,
    followingCount: 0
  };
  isLoadingStats = false;

  feedType: 'all' | 'following' = 'following';

  constructor(
    private router: Router,
    private authService: AuthService,
    private postsService: PostsService,
    private followsService: FollowsService,
    private likesService: LikesService,
    private toastService: ToastService
  ) {}
  


  trendingTopics: TrendingTopic[] = [
    { tag: '#WebDevelopment', posts: '1.2K' },
    { tag: '#TechNews', posts: '890' },
    { tag: '#AI', posts: '2.5K' },
    { tag: '#JavaScript', posts: '1.8K' },
    { tag: '#Design', posts: '756' }
  ];

  ngOnInit(): void {
    // Load current user data
    this.loadUserData();
    
    // Load posts
    this.loadPosts();
  }

  loadUserData(): void {
    // Subscribe to current user from auth service
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUser = {
          name: user.username || user.name || 'User',
          email: user.email || '',
          initials: this.getInitials(user.username || user.name || user.email || 'U'),
          role: user.role || 'User',
          id: user.id || ''
        };
        
        // Load user statistics once we have the user ID
        if (this.currentUser.id) {
          this.loadUserStatistics();
        }
      } else {
        // Fallback: Try to get from localStorage
        const storedUser = localStorage.getItem('user');
        if (storedUser) {
          const parsedUser = JSON.parse(storedUser);
          this.currentUser = {
            name: parsedUser.username || parsedUser.name || 'User',
            email: parsedUser.email || '',
            initials: this.getInitials(parsedUser.username || parsedUser.name || parsedUser.email || 'U'),
            role: parsedUser.role || 'User',
            id: parsedUser.id || ''
          };
        }
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

  loadPosts(): void {
    this.isLoading = true;
    
    const postsObservable = this.feedType === 'following' 
      ? this.postsService.getPostsFromFollowing()
      : this.postsService.getRecentPosts(20);

    postsObservable.subscribe({
      next: (posts) => {
        this.posts = posts.map(post => this.transformPostForDisplay(post));
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading posts:', error);
        this.isLoading = false;
        // Fallback to empty array or show error message
        this.posts = [];
        if (this.feedType === 'following') {
          this.toastService.info('No Posts', 'No posts from followed users yet');
        }
      }
    });
  }

  switchFeed(type: 'all' | 'following'): void {
    this.feedType = type;
    this.loadPosts();
  }

  private transformPostForDisplay(apiPost: any): Post {
    // Use backend author information if available, otherwise fallback to current user
    const authorName = apiPost.authorName || apiPost.author?.name || 'Unknown User';
    const authorUserName = apiPost.authorUserName || apiPost.author?.username || '';
    
    return {
      ...apiPost,
      timeAgo: this.calculateTimeAgo(apiPost.createdAt),
      author: {
        name: authorName,
        initials: this.getInitials(authorName),
        userId: apiPost.userId
      }
    };
  }

  private transformNewPostForDisplay(apiPost: any): Post {
    // For newly created posts, use current user info if author info is not available
    const authorName = apiPost.authorName || apiPost.author?.name || this.currentUser.name || 'Unknown User';
    const authorUserName = apiPost.authorUserName || apiPost.author?.username || this.currentUser.email || '';
    
    return {
      ...apiPost,
      timeAgo: this.calculateTimeAgo(apiPost.createdAt),
      author: {
        name: authorName,
        initials: this.getInitials(authorName),
        userId: apiPost.userId
      }
    };
  }

  private calculateTimeAgo(createdAt: string): string {
    const now = new Date();
    const postDate = new Date(createdAt);
    const diffInSeconds = Math.floor((now.getTime() - postDate.getTime()) / 1000);

    if (diffInSeconds < 60) return 'Just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
  }

  likePost(postId: string): void {
    const post = this.posts.find(p => p.postId === postId);
    if (!post) {
      return;
    }

    // Optimistic update
    const wasLiked = post.isLikedByCurrentUser;
    post.isLikedByCurrentUser = !wasLiked;
    post.likesCount += post.isLikedByCurrentUser ? 1 : -1;

    // Call API to toggle like
    this.likesService.toggleLike(postId).subscribe({
      next: (response) => {
        // Update with actual server response
        post.isLikedByCurrentUser = response.isLiked ?? post.isLikedByCurrentUser;
        post.likesCount = response.likesCount;
      },
      error: (error) => {
        console.error('Error toggling like:', error);
        // Revert optimistic update on error
        post.isLikedByCurrentUser = wasLiked;
        post.likesCount += post.isLikedByCurrentUser ? 1 : -1;
        this.toastService.error('Error', 'Failed to update like. Please try again.');
      }
    });
  }

  createPost(): void {
    if (!this.newPost.content.trim()) {
      alert('Please enter some content for your post');
      return;
    }

    if (!this.currentUser.id) {
      alert('User not authenticated. Please log in again.');
      return;
    }

    this.isCreatingPost = true;

    // Determine image URL - use preview if file selected, otherwise use manual URL
    let imageUrl = this.newPost.imageUrl.trim() || undefined;
    if (this.imagePreview && this.selectedFile) {
      imageUrl = this.imagePreview; // Use data URL for now
    }

    const createPostRequest: CreatePostRequest = {
      userId: this.currentUser.id,
      content: this.newPost.content.trim(),
      imageUrl: ""
    };

    this.postsService.createPost(createPostRequest).subscribe({
      next: (newPost) => {
        // For newly created posts, use current user info if author info is not available
        const transformedPost = this.transformNewPostForDisplay(newPost);
        // Add to the beginning of the posts array
        this.posts.unshift(transformedPost);
        
        // Reset form
        this.clearForm();
        
        this.isCreatingPost = false;
        this.toastService.success('Success!', 'Your post has been created successfully.');
        console.log('Post created successfully:', newPost);
        
        // Update user statistics (posts count)
        this.loadUserStatistics();
      },
      error: (error) => {
        console.error('Error creating post:', error);
        this.isCreatingPost = false;
        this.toastService.error('Error', 'Failed to create post. Please try again.');
      }
    });
  }

  onImageUrlChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.newPost.imageUrl = target.value;
  }

  clearForm(): void {
    this.newPost = {
      content: '',
      imageUrl: ''
    };
    this.selectedFile = null;
    this.imagePreview = null;
  }

  onPhotoClick(): void {
    const fileInput = document.getElementById('photo-upload') as HTMLInputElement;
    fileInput?.click();
  }

  onFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    
    if (file) {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        alert('Please select an image file (JPG, PNG, GIF, etc.)');
        return;
      }

      // Validate file size (5MB limit)
      const maxSize = 5 * 1024 * 1024; // 5MB
      if (file.size > maxSize) {
        alert('File size must be less than 5MB');
        return;
      }

      this.selectedFile = file;
      
      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  removeSelectedImage(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    this.newPost.imageUrl = '';
    
    // Reset file input
    const fileInput = document.getElementById('photo-upload') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  // Post dropdown methods
  togglePostDropdown(postId: string): void {
    this.openDropdowns[postId] = !this.openDropdowns[postId];
  }

  isDropdownOpen(postId: string): boolean {
    return this.openDropdowns[postId] || false;
  }

  // Post editing methods
  onEditPost(post: Post): void {
    this.editingPost = post;
    this.editPostForm = {
      content: post.content,
      imageUrl: post.imageUrl || ''
    };
    this.isEditingPost = true;
  }

  onCancelEdit(): void {
    this.editingPost = null;
    this.editPostForm = {
      content: '',
      imageUrl: ''
    };
    this.isEditingPost = false;
  }

  onSaveEdit(): void {
    if (!this.editingPost || !this.editPostForm.content.trim()) {
      return;
    }

    this.isSavingPost = true;

    const updateRequest: UpdatePostRequest = {
      content: this.editPostForm.content.trim(),
      imageUrl: this.editPostForm.imageUrl || undefined
    };

    this.postsService.updatePost(this.editingPost.postId, updateRequest, this.currentUser.id).subscribe({
      next: (updatedPost) => {
        // Find and update the post in the array
        const index = this.posts.findIndex(p => p.postId === this.editingPost!.postId);
        if (index !== -1) {
          this.posts[index] = this.transformPostForDisplay(updatedPost);
        }
        
        this.onCancelEdit();
        this.isSavingPost = false;
        this.toastService.success('Success!', 'Your post has been updated successfully.');
        console.log('Post updated successfully:', updatedPost);
      },
      error: (error) => {
        console.error('Error updating post:', error);
        this.isSavingPost = false;
        this.toastService.error('Error', 'Failed to update post. Please try again.');
      }
    });
  }

  // Post deletion methods
  onDeletePost(post: Post): void {
    this.postToDelete = post;
    this.showDeleteModal = true;
  }

  onConfirmDelete(): void {
    if (!this.postToDelete) return;

    this.isDeletingPost = true;

    this.postsService.deletePost(this.postToDelete.postId, this.currentUser.id).subscribe({
      next: () => {
        // Remove the post from the array
        this.posts = this.posts.filter(p => p.postId !== this.postToDelete!.postId);
        this.isDeletingPost = false;
        this.showDeleteModal = false;
        this.postToDelete = null;
        this.toastService.success('Success!', 'Your post has been deleted successfully.');
        console.log('Post deleted successfully');
        
        // Update user statistics (posts count)
        this.loadUserStatistics();
      },
      error: (error) => {
        console.error('Error deleting post:', error);
        this.isDeletingPost = false;
        this.showDeleteModal = false;
        this.postToDelete = null;
        this.toastService.error('Error', 'Failed to delete post. Please try again.');
      }
    });
  }

  onCancelDelete(): void {
    this.showDeleteModal = false;
    this.postToDelete = null;
  }

  onLogout(): void {
    // Use auth service logout
    this.authService.logout();
    
    // Navigate to login page
    this.router.navigate(['/login']);
  }

  navigateToSearch(): void {
    this.router.navigate(['/search']);
  }

  onFollowStatusChanged(): void {
    // Reload posts if on following feed
    if (this.feedType === 'following') {
      this.loadPosts();
    }
    // Reload user statistics to update counts after follow/unfollow
    if (this.currentUser.id) {
      this.loadUserStatistics();
    }
  }

  loadUserStatistics(): void {
    if (!this.currentUser.id) {
      return;
    }

    this.isLoadingStats = true;

    // Load posts count
    this.postsService.getPostsByUser(this.currentUser.id).subscribe({
      next: (posts) => {
        this.userStats.postsCount = posts.length;
        this.isLoadingStats = false;
      },
      error: (error) => {
        console.error('Error loading posts count:', error);
        this.userStats.postsCount = 0;
        this.isLoadingStats = false;
      }
    });

    // Load followers count
    this.followsService.getFollowerCount(this.currentUser.id).subscribe({
      next: (response) => {
        this.userStats.followersCount = response.count;
      },
      error: (error) => {
        console.error('Error loading followers count:', error);
        this.userStats.followersCount = 0;
      }
    });

    // Load following count
    this.followsService.getFollowingCount(this.currentUser.id).subscribe({
      next: (response) => {
        this.userStats.followingCount = response.count;
      },
      error: (error) => {
        console.error('Error loading following count:', error);
        this.userStats.followingCount = 0;
      }
    });
  }

  formatCount(count: number): string {
    if (count >= 1000) {
      return (count / 1000).toFixed(1) + 'K';
    }
    return count.toString();
  }
}
