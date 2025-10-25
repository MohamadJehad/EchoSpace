import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Post {
  id: number;
  author: {
    name: string;
    initials: string;
  };
  timeAgo: string;
  content: string;
  imageUrl?: string;
  likes: number;
  comments: number;
  liked: boolean;
}

interface SuggestedUser {
  id: number;
  name: string;
  initials: string;
  mutualFriends: number;
}

interface TrendingTopic {
  tag: string;
  posts: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  isLoading = false;
  
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
    // Simulate loading posts
    this.loadPosts();
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
}
