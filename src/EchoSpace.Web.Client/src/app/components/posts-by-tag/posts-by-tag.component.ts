import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { PostsService } from '../../services/posts.service';
import { TagsService } from '../../services/tags.service';
import { LikesService } from '../../services/likes.service';
import { FollowsService } from '../../services/follows.service';
import { AuthService } from '../../services/auth.service';
import { UserService, User } from '../../services/user.service';
import { ToastService } from '../../services/toast.service';
import { Post } from '../../interfaces';
import { Tag } from '../../interfaces/tag.interface';
import { PostDropdownComponent } from '../post-dropdown/post-dropdown.component';
import { PostCommentsComponent } from '../post-comments/post-comments.component';
import { NavbarDropdownComponent } from '../navbar-dropdown/navbar-dropdown.component';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-posts-by-tag',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    PostDropdownComponent,
    PostCommentsComponent,
    NavbarDropdownComponent,
    SearchBarComponent
  ],
  templateUrl: './posts-by-tag.component.html',
  styleUrl: './posts-by-tag.component.css'
})
export class PostsByTagComponent implements OnInit {
  tagId: string = '';
  tag: Tag | null = null;
  posts: Post[] = [];
  isLoading = false;
  isLoadingTag = false;
  
  currentUser = {
    name: '',
    email: '',
    initials: '',
    role: '',
    id: '',
    profilePhotoUrl: null as string | null
  };

  openDropdowns: { [postId: string]: boolean } = {};
  showComments: { [postId: string]: boolean } = {};
  profilePhotoCache: { [userId: string]: string | null } = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private postsService: PostsService,
    private tagsService: TagsService,
    private likesService: LikesService,
    private followsService: FollowsService,
    private authService: AuthService,
    private userService: UserService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUserData();
    this.route.paramMap.subscribe(params => {
      this.tagId = params.get('tagId') || '';
      if (this.tagId) {
        this.loadTag();
        this.loadPosts();
      }
    });
  }

  loadUserData(): void {
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
        if (this.currentUser.id) {
          this.loadUserProfile();
        }
      }
    });
  }

  loadUserProfile(): void {
    if (!this.currentUser.id) return;
    
    this.userService.getCurrentUser().subscribe({
      next: (user: User) => {
        if (user.name) {
          this.currentUser.name = user.name;
          this.currentUser.initials = this.getInitials(user.name);
        }
        if (user.email) {
          this.currentUser.email = user.email;
        }
        
        const profilePhotoId = user.profilePhotoId;
        if (profilePhotoId) {
          // Fetch the profile photo URL through the images endpoint (same as home component)
          fetch(`${environment.apiUrl}/images/${profilePhotoId}/url`, {
            headers: {
              'Authorization': `Bearer ${this.authService.getToken()}`
            }
          })
          .then(response => response.json())
          .then(data => {
            this.currentUser.profilePhotoUrl = data.url;
            this.profilePhotoCache[this.currentUser.id] = data.url;
          })
          .catch(error => {
            console.error('Error loading current user profile photo:', error);
          });
        }
      },
      error: (error) => {
        console.error('Error loading user profile:', error);
      }
    });
  }

  loadTag(): void {
    this.isLoadingTag = true;
    this.tagsService.getTagById(this.tagId).subscribe({
      next: (tag) => {
        this.tag = tag;
        this.isLoadingTag = false;
      },
      error: (error) => {
        console.error('Error loading tag:', error);
        this.isLoadingTag = false;
        this.toastService.error('Error', 'Failed to load tag');
      }
    });
  }

  loadPosts(): void {
    this.isLoading = true;
    this.postsService.getPostsByTag(this.tagId).subscribe({
      next: (posts) => {
        // Transform posts first
        this.posts = posts.map(post => this.transformPostForDisplay(post));
        // Then load profile photos (which will update the posts as they load)
        this.loadProfilePhotosForPosts();
        this.isLoading = false;
        // Trigger change detection after initial load
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading posts:', error);
        this.isLoading = false;
        this.posts = [];
        this.toastService.error('Error', 'Failed to load posts');
      }
    });
  }

  private transformPostForDisplay(apiPost: Partial<Post> & { userId: string; createdAt: string; postId: string }): Post {
    // Use backend author information if available, otherwise fallback to current user
    const authorName = apiPost.authorName || apiPost.author?.name || 'Unknown User';
    const userId = apiPost.userId;
    
    // Check if we already have profile photo in cache
    const profilePhotoUrl = this.profilePhotoCache[userId] || null;
    
    // If it's the current user, use their profile photo
    if (userId === this.currentUser.id && this.currentUser.profilePhotoUrl) {
      this.profilePhotoCache[userId] = this.currentUser.profilePhotoUrl;
    }

    return {
      ...apiPost,
      postId: apiPost.postId || '',
      userId: apiPost.userId,
      content: apiPost.content || '',
      timeAgo: this.getTimeAgo(apiPost.createdAt),
      author: {
        name: authorName,
        initials: this.getInitials(authorName),
        userId: userId,
        profilePhotoUrl: profilePhotoUrl || (userId === this.currentUser.id ? this.currentUser.profilePhotoUrl : null)
      },
      authorName: authorName
    } as Post;
  }

  private loadProfilePhotosForPosts(): void {
    // Get unique user IDs from posts (use author.userId like home component)
    const userIds = [...new Set(
      this.posts
        .map(post => post.author?.userId || post.userId)
        .filter(id => id && id !== this.currentUser.id)
    )];
    
    console.log('loadProfilePhotosForPosts - User IDs:', userIds);
    console.log('loadProfilePhotosForPosts - Posts:', this.posts);
    
    // Load profile photos for each unique user that doesn't have a cached photo
    userIds.forEach(userId => {
      if (userId && !this.profilePhotoCache[userId]) {
        console.log(`Loading profile photo for user: ${userId}`);
        this.loadAuthorProfilePhoto(userId);
      } else if (userId && this.profilePhotoCache[userId]) {
        // If we already have it cached, update the posts immediately
        const photoUrl = this.profilePhotoCache[userId];
        console.log(`Using cached profile photo for user: ${userId}`, photoUrl);
        this.posts = this.posts.map(post => {
          const postUserId = post.author?.userId || post.userId;
          if (postUserId === userId && post.author) {
            console.log(`Updated post ${post.postId} with cached profile photo`);
            return {
              ...post,
              author: {
                ...post.author,
                profilePhotoUrl: photoUrl
              }
            };
          }
          return post;
        });
      }
    });
    
    // Trigger change detection after checking cache
    this.cdr.detectChanges();
  }

  private loadAuthorProfilePhoto(userId: string): void {
    this.userService.getUserById(userId).subscribe({
      next: (user: User) => {
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
    console.log(`loadProfilePhotoUrlForAuthor - userId: ${userId}, imageId: ${imageId}`);
    // environment.apiUrl already includes /api, so we don't need to add it again
    fetch(`${environment.apiUrl}/images/${imageId}/url`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      }
    })
    .then(response => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then(data => {
      // Cache the profile photo URL
      const photoUrl = data.url;
      console.log(`Profile photo URL loaded for user ${userId}:`, photoUrl);
      this.profilePhotoCache[userId] = photoUrl;
      
      // Update all posts with this author's profile photo
      let updated = false;
      this.posts = this.posts.map(post => {
        const postUserId = post.author?.userId || post.userId;
        if (postUserId === userId && post.author) {
          console.log(`Updating post ${post.postId} author profile photo`);
          updated = true;
          return {
            ...post,
            author: {
              ...post.author,
              profilePhotoUrl: photoUrl
            }
          };
        }
        return post;
      });
      
      console.log(`Updated ${updated ? 'posts' : 'no posts'} with profile photo for user ${userId}`);
      console.log('Posts after update:', this.posts);
      
      // Always trigger change detection when we get a new photo
      if (updated) {
        this.cdr.detectChanges();
      }
    })
    .catch(error => {
      console.error(`Error loading profile photo URL for user ${userId}:`, error);
      // Don't cache failed requests
    });
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

  onCommentsCountChanged(postId: string, newCount: number): void {
    const post = this.posts.find(p => p.postId === postId);
    if (post) {
      post.commentsCount = newCount;
    }
  }

  likePost(postId: string): void {
    this.likesService.likePost(postId).subscribe({
      next: () => {
        const post = this.posts.find(p => p.postId === postId);
        if (post) {
          post.isLikedByCurrentUser = true;
          post.likesCount++;
        }
      },
      error: (error) => {
        console.error('Error liking post:', error);
        this.toastService.error('Error', 'Failed to like post');
      }
    });
  }

  unlikePost(postId: string): void {
    this.likesService.unlikePost(postId).subscribe({
      next: () => {
        const post = this.posts.find(p => p.postId === postId);
        if (post) {
          post.isLikedByCurrentUser = false;
          post.likesCount--;
        }
      },
      error: (error) => {
        console.error('Error unliking post:', error);
        this.toastService.error('Error', 'Failed to unlike post');
      }
    });
  }

  navigateToSearch(): void {
    this.router.navigate(['/search']);
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    const words = name.trim().split(/\s+/);
    if (words.length === 1) {
      return words[0].substring(0, 2).toUpperCase();
    }
    return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
  }

  getTimeAgo(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (seconds < 60) return 'just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d ago`;
    const weeks = Math.floor(days / 7);
    if (weeks < 4) return `${weeks}w ago`;
    const months = Math.floor(days / 30);
    return `${months}mo ago`;
  }
}

