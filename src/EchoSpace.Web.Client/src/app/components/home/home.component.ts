import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PostsService } from '../../services/posts.service';
import { FollowsService } from '../../services/follows.service';
import { LikesService } from '../../services/likes.service';
import { TagsService } from '../../services/tags.service';
import { NavbarDropdownComponent } from '../navbar-dropdown/navbar-dropdown.component';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { SuggestedUsersComponent } from '../suggested-users/suggested-users.component';
import { PostDropdownComponent } from '../post-dropdown/post-dropdown.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { ToastService } from '../../services/toast.service';
import { Post, TrendingTopic, CreatePostRequest, UpdatePostRequest } from '../../interfaces';
import { PostCommentsComponent } from '../post-comments/post-comments.component';
import { UserService } from '../../services/user.service';
import { ProfileCardComponent } from '../profile-card/profile-card.component';
import { environment } from '../../../environments/environment';
import { normalizeRole } from '../../utils/role.util';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, NavbarDropdownComponent, SearchBarComponent, SuggestedUsersComponent, PostDropdownComponent, ConfirmationModalComponent, PostCommentsComponent, ProfileCardComponent],
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
    imageUrl: '',
    tagIds: [] as string[],
    generateImage: false
  };
  
  // Tags
  tags: any[] = [];
  isLoadingTags = false;
  showTagSelector = false;

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
  
  showComments: { [postId: string]: boolean } = {};

  // Translation states
  translatingPosts: { [postId: string]: boolean } = {};
  translationLanguage: string = 'en'; // Default to English
  showLanguageSelector: { [postId: string]: boolean } = {};
  
  // Summarization state
  summarizingPosts: { [postId: string]: boolean } = {};
  
  // Available languages for translation
  availableLanguages = [
    { code: 'en', name: 'English' },
    { code: 'ar', name: 'Arabic' },
    { code: 'fr', name: 'French' },
    { code: 'es', name: 'Spanish' },
    { code: 'de', name: 'German' },
    { code: 'it', name: 'Italian' },
    { code: 'pt', name: 'Portuguese' },
    { code: 'ru', name: 'Russian' },
    { code: 'zh', name: 'Chinese' },
    { code: 'ja', name: 'Japanese' },
    { code: 'ko', name: 'Korean' },
    { code: 'hi', name: 'Hindi' },
    { code: 'tr', name: 'Turkish' },
    { code: 'nl', name: 'Dutch' },
    { code: 'pl', name: 'Polish' }
  ];

  currentUser = {
    name: 'John Doe',
    email: 'john.doe@example.com',
    initials: 'JD',
    role: 'User',
    id: '',
    profilePhotoUrl: null as string | null
  };

  // User statistics
  userStats = {
    postsCount: 0,
    followersCount: 0,
    followingCount: 0
  };
  isLoadingStats = false;

  feedType: 'all' | 'following' = 'following';

  // Cache for profile photos by user ID
  profilePhotoCache: { [userId: string]: string } = {};

  constructor(
    private router: Router,
    private authService: AuthService,
    private postsService: PostsService,
    private followsService: FollowsService,
    private likesService: LikesService,
    private toastService: ToastService,
    private userService: UserService,
    private tagsService: TagsService
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
    
    // Load tags
    this.loadTags();
    
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
          id: user.id || '',
          profilePhotoUrl: null
        };
        
        // Redirect Operation users to their homepage
        const normalizedRole = normalizeRole(this.currentUser.role);
        if (normalizedRole === 'Operation') {
          this.router.navigate(['/operation']);
          return;
        }
        
        // Load user statistics once we have the user ID
        if (this.currentUser.id) {
          this.loadUserStatistics();
          this.loadUserProfile();
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
            id: parsedUser.id || '',
            profilePhotoUrl: null
          };
          
          // Redirect Operation users to their homepage
          const normalizedRole = normalizeRole(this.currentUser.role);
          if (normalizedRole === 'Operation') {
            this.router.navigate(['/operation']);
            return;
          }
          
          if (this.currentUser.id) {
            this.loadUserProfile();
          }
        }
      }
    });
  }

  loadUserProfile(): void {
    if (!this.currentUser.id) {
      console.log('loadUserProfile: No user ID available');
      return;
    }
    
    console.log('loadUserProfile: Loading user profile for ID:', this.currentUser.id);
    
    this.userService.getCurrentUser().subscribe({
      next: (user: any) => {
        console.log('loadUserProfile: Received user data:', user);
        
        // Update user info if needed
        if (user.name) {
          this.currentUser.name = user.name;
          this.currentUser.initials = this.getInitials(user.name);
        }
        if (user.email) {
          this.currentUser.email = user.email;
        }
        
        // Handle both camelCase and PascalCase property names
        const profilePhotoId = user.profilePhotoId || user.ProfilePhotoId;
        
        console.log('loadUserProfile: Profile photo ID:', profilePhotoId);
        
        // Load profile photo if exists
        if (profilePhotoId) {
          console.log('loadUserProfile: Loading profile photo URL for imageId:', profilePhotoId);
          this.loadProfilePhotoUrl(profilePhotoId);
        } else {
          console.log('loadUserProfile: No profile photo ID found');
          // Clear profile photo if none exists
          this.currentUser.profilePhotoUrl = null;
          if (this.currentUser.id) {
            delete this.profilePhotoCache[this.currentUser.id];
          }
        }
      },
      error: (error) => {
        console.error('Error loading user profile:', error);
        console.error('Error details:', JSON.stringify(error, null, 2));
      }
    });
  }

  loadProfilePhotoUrl(imageId: string): void {
    if (!imageId) {
      console.warn('loadProfilePhotoUrl: No imageId provided');
      return;
    }
    
    console.log('loadProfilePhotoUrl: Loading URL for imageId:', imageId);
    // environment.apiUrl already includes /api, so we don't need to add it again
    const url = `${environment.apiUrl}/images/${imageId}/url`;
    console.log('loadProfilePhotoUrl: Fetching from:', url);
    
    fetch(url, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      }
    })
    .then(response => {
      console.log('loadProfilePhotoUrl: Response status:', response.status);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then(data => {
      console.log('loadProfilePhotoUrl: Received data:', data);
      if (data && data.url) {
        // Only update if we got a valid URL
        console.log('loadProfilePhotoUrl: Setting profile photo URL:', data.url);
        this.currentUser.profilePhotoUrl = data.url;
        
        // Update cache for current user
        if (this.currentUser.id) {
          this.profilePhotoCache[this.currentUser.id] = data.url;
          console.log('loadProfilePhotoUrl: Updated cache for user:', this.currentUser.id);
        }
        
        // Update all posts by current user
        this.posts.forEach(post => {
          if (post.author?.userId === this.currentUser.id) {
            if (post.author) {
              post.author.profilePhotoUrl = data.url;
            }
          }
        });
        console.log('loadProfilePhotoUrl: Updated', this.posts.filter(p => p.author?.userId === this.currentUser.id).length, 'posts with new profile photo');
      } else {
        console.warn('loadProfilePhotoUrl: Response did not contain a valid URL:', data);
      }
    })
    .catch(error => {
      console.error('loadProfilePhotoUrl: Error loading profile photo URL:', error);
      console.error('loadProfilePhotoUrl: Error details:', error.message);
      // Don't clear the existing URL on error - keep what we have
    });
  }

  onProfilePhotoUpdated(imageUrl: string): void {
    // Handle profile photo update from profile card component
    console.log('onProfilePhotoUpdated: Profile photo updated:', imageUrl);
    
    // Update cache for current user
    if (this.currentUser.id) {
      this.profilePhotoCache[this.currentUser.id] = imageUrl;
    }
    
    // Update all posts by current user to show new profile photo
    this.posts.forEach(post => {
      if (post.author?.userId === this.currentUser.id) {
        if (post.author) {
          post.author.profilePhotoUrl = imageUrl;
        }
      }
    });
    console.log('onProfilePhotoUpdated: Updated posts count:', this.posts.filter(p => p.author?.userId === this.currentUser.id).length);
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
        // Load profile photos for all post authors
        this.loadProfilePhotosForPosts(this.posts);
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
    const userId = apiPost.userId;
    
    // Check if we already have profile photo in cache
    const profilePhotoUrl = this.profilePhotoCache[userId] || null;
    
    // If it's the current user, use their profile photo
    if (userId === this.currentUser.id && this.currentUser.profilePhotoUrl) {
      this.profilePhotoCache[userId] = this.currentUser.profilePhotoUrl;
    }
    
    return {
      ...apiPost,
      timeAgo: this.calculateTimeAgo(apiPost.createdAt),
      author: {
        name: authorName,
        initials: this.getInitials(authorName),
        userId: userId,
        profilePhotoUrl: profilePhotoUrl || (userId === this.currentUser.id ? this.currentUser.profilePhotoUrl : null)
      },
      authorProfilePhotoId: apiPost.authorProfilePhotoId
    };
  }

  private transformNewPostForDisplay(apiPost: any): Post {
    // For newly created posts, use current user info if author info is not available
    const authorName = apiPost.authorName || apiPost.author?.name || this.currentUser.name || 'Unknown User';
    const authorUserName = apiPost.authorUserName || apiPost.author?.username || this.currentUser.email || '';
    const userId = apiPost.userId || this.currentUser.id;
    
    return {
      ...apiPost,
      timeAgo: this.calculateTimeAgo(apiPost.createdAt),
      author: {
        name: authorName,
        initials: this.getInitials(authorName),
        userId: userId,
        profilePhotoUrl: userId === this.currentUser.id ? this.currentUser.profilePhotoUrl : null
      }
    };
  }

  private loadProfilePhotosForPosts(posts: Post[]): void {
    // Get unique user IDs from posts
    const userIds = [...new Set(posts.map(post => post.author?.userId).filter(id => id && id !== this.currentUser.id))];
    
    // Load profile photos for each unique user
    userIds.forEach(userId => {
      if (userId && !this.profilePhotoCache[userId]) {
        this.loadAuthorProfilePhoto(userId);
      }
    });
  }

  private loadAuthorProfilePhoto(userId: string): void {
    this.userService.getUserById(userId).subscribe({
      next: (user: any) => {
        if (user.profilePhotoId) {
          this.loadProfilePhotoUrlForAuthor(userId, user.profilePhotoId);
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
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      }
    })
    .then(response => response.json())
    .then(data => {
      // Cache the profile photo URL
      this.profilePhotoCache[userId] = data.url;
      
      // Update all posts with this author's profile photo
      this.posts.forEach(post => {
        if (post.author?.userId === userId) {
          if (post.author) {
            post.author.profilePhotoUrl = data.url;
          }
        }
      });
    })
    .catch(error => {
      console.error(`Error loading profile photo URL for user ${userId}:`, error);
    });
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

  toggleLanguageSelector(postId: string, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.showLanguageSelector[postId] = !this.showLanguageSelector[postId];
  }

  closeLanguageSelector(postId: string): void {
    this.showLanguageSelector[postId] = false;
  }

  closeAllLanguageSelectors(): void {
    Object.keys(this.showLanguageSelector).forEach(key => {
      this.showLanguageSelector[key] = false;
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    // Close language selectors when clicking outside
    const target = event.target as HTMLElement;
    const isLanguageSelector = target.closest('.language-selector-container');
    if (!isLanguageSelector) {
      this.closeAllLanguageSelectors();
    }
  }

  translatePost(postId: string, language?: string): void {
    if (this.translatingPosts[postId]) {
      return; // Already translating
    }

    const post = this.posts.find(p => p.postId === postId);
    if (!post) {
      return;
    }

    // If already translated, show original
    if (post.isTranslated && post.translatedContent && !language) {
      post.isTranslated = false;
      post.translatedContent = undefined;
      post.translationLanguage = undefined;
      this.showLanguageSelector[postId] = false;
      // Also clear summarization state if it was set
      if (post.isSummarized) {
        post.isSummarized = false;
        post.summarizedContent = undefined;
      }
      return;
    }

    // Use provided language or default
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

  getLanguageName(code: string): string {
    const lang = this.availableLanguages.find(l => l.code === code);
    return lang ? lang.name : code.toUpperCase();
  }

  summarizePost(postId: string): void {
    if (this.summarizingPosts[postId]) {
      return; // Already summarizing
    }

    const post = this.posts.find(p => p.postId === postId);
    if (!post) {
      return;
    }

    // If already summarized, show original
    if (post.isSummarized && post.summarizedContent) {
      post.isSummarized = false;
      post.summarizedContent = undefined;
      // Also clear translation state if it was set
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
      imageUrl: "",
      tagIds: this.newPost.tagIds.length > 0 ? this.newPost.tagIds : undefined,
      generateImage: this.newPost.generateImage
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
        this.toastService.error('Error', 'Failed to create post. Post contains unsafe or toxic contents. Please fix post contents and try again.');
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
      imageUrl: '',
      tagIds: [],
      generateImage: false
    };
    this.selectedFile = null;
    this.imagePreview = null;
  }
  
  toggleTagSelection(tagId: string): void {
    const index = this.newPost.tagIds.indexOf(tagId);
    if (index > -1) {
      this.newPost.tagIds.splice(index, 1);
    } else {
      this.newPost.tagIds.push(tagId);
    }
  }
  
  isTagSelected(tagId: string): boolean {
    return this.newPost.tagIds.includes(tagId);
  }
  
  loadTags(): void {
    this.isLoadingTags = true;
    this.tagsService.getAllTags().subscribe({
      next: (tags) => {
        this.tags = tags;
        this.isLoadingTags = false;
      },
      error: (error) => {
        console.error('Error loading tags:', error);
        this.isLoadingTags = false;
      }
    });
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
        this.toastService.error('Error', 'Failed to update post. Contents of the post are not safe. Please fix contents and try again.');
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

  // Comment methods
  toggleComments(postId: string): void {
    this.showComments[postId] = !this.showComments[postId];
  }

  onCommentsCountChanged(postId: string, newCount: number): void {
    const post = this.posts.find(p => p.postId === postId);
    if (post) {
      post.commentsCount = newCount;
    }
  }
}
