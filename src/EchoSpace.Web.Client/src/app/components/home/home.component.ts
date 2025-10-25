import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PostsService } from '../../services/posts.service';
import { NavbarDropdownComponent } from '../navbar-dropdown/navbar-dropdown.component';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { SuggestedUsersComponent } from '../suggested-users/suggested-users.component';
import { Post, TrendingTopic, CreatePostRequest } from '../../interfaces';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, NavbarDropdownComponent, SearchBarComponent, SuggestedUsersComponent],
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
  
  currentUser = {
    name: 'John Doe',
    email: 'john.doe@example.com',
    initials: 'JD',
    role: 'User',
    id: ''
  };

  constructor(
    private router: Router,
    private authService: AuthService,
    private postsService: PostsService
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
    
    // Simulate loading posts
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
    this.postsService.getRecentPosts(20).subscribe({
      next: (posts) => {
        this.posts = posts.map(post => this.transformPostForDisplay(post));
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading posts:', error);
        this.isLoading = false;
        // Fallback to empty array or show error message
        this.posts = [];
      }
    });
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
    if (post) {
      // TODO: Implement like/unlike API call
      post.isLikedByCurrentUser = !post.isLikedByCurrentUser;
      post.likesCount += post.isLikedByCurrentUser ? 1 : -1;
    }
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
        console.log('Post created successfully:', newPost);
      },
      error: (error) => {
        console.error('Error creating post:', error);
        this.isCreatingPost = false;
        alert('Failed to create post. Please try again.');
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

  onLogout(): void {
    // Use auth service logout
    this.authService.logout();
    
    // Navigate to login page
    this.router.navigate(['/login']);
  }

  navigateToSearch(): void {
    this.router.navigate(['/search']);
  }
}
