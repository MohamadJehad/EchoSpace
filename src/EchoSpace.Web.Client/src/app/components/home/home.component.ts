import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NavbarDropdownComponent } from '../navbar-dropdown/navbar-dropdown.component';
import { Post, SuggestedUser, TrendingTopic } from '../../interfaces';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarDropdownComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  isLoading = false;
  
  currentUser = {
    name: 'John Doe',
    email: 'john.doe@example.com',
    initials: 'JD',
    role: 'User'
  };

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}
  
  posts: Post[] = [
    {
      id: 1,
      author: {
        name: 'Sarah Johnson',
        initials: 'SJ'
      },
      timeAgo: '2 hours ago',
      content: 'Just finished an amazing project! 🎉 The journey was challenging but incredibly rewarding. Grateful for the amazing team that made this possible. #TechLife #Development',
      imageUrl: 'https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=800&auto=format&fit=crop',
      likes: 124,
      comments: 18,
      liked: false
    },
    {
      id: 2,
      author: {
        name: 'Michael Chen',
        initials: 'MC'
      },
      timeAgo: '4 hours ago',
      content: 'Beautiful sunset at the beach today! 🌅 Sometimes you need to take a break and appreciate the simple things in life.',
      imageUrl: 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&auto=format&fit=crop',
      likes: 256,
      comments: 34,
      liked: true
    },
    {
      id: 3,
      author: {
        name: 'Emma Davis',
        initials: 'ED'
      },
      timeAgo: '6 hours ago',
      content: 'Excited to announce that I\'ll be speaking at TechConf 2025! Can\'t wait to share insights about modern web development and meet fellow developers. See you there! 🚀',
      likes: 89,
      comments: 12,
      liked: false
    },
    {
      id: 4,
      author: {
        name: 'Alex Rodriguez',
        initials: 'AR'
      },
      timeAgo: '8 hours ago',
      content: 'Coffee + Code = Perfect Morning ☕️ Working on something exciting. Stay tuned for updates!',
      imageUrl: 'https://images.unsplash.com/photo-1461749280684-dccba630e2f6?w=800&auto=format&fit=crop',
      likes: 167,
      comments: 23,
      liked: false
    },
    {
      id: 5,
      author: {
        name: 'Sophie Turner',
        initials: 'ST'
      },
      timeAgo: '10 hours ago',
      content: 'Just launched my new portfolio website! Check it out and let me know what you think. Your feedback means the world to me! 💻✨',
      likes: 203,
      comments: 45,
      liked: true
    }
  ];

  suggestedUsers: SuggestedUser[] = [
    { id: 1, name: 'David Wilson', initials: 'DW', mutualFriends: 12 },
    { id: 2, name: 'Lisa Anderson', initials: 'LA', mutualFriends: 8 },
    { id: 3, name: 'Ryan Martinez', initials: 'RM', mutualFriends: 15 },
    { id: 4, name: 'Jessica Lee', initials: 'JL', mutualFriends: 6 }
  ];

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
          role: user.role || 'User'
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
            role: parsedUser.role || 'User'
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
    // In a real app, this would fetch posts from an API
    console.log('Posts loaded');
  }

  likePost(postId: number): void {
    const post = this.posts.find(p => p.id === postId);
    if (post) {
      post.liked = !post.liked;
      post.likes += post.liked ? 1 : -1;
    }
  }

  onLogout(): void {
    // Use auth service logout
    this.authService.logout();
    
    // Navigate to login page
    this.router.navigate(['/login']);
  }
}
