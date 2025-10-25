export interface Post {
  postId: string; 
  userId: string; 
  author: {
    name: string;
    initials: string;
    userId?: string; 
  };
  content: string;
  imageUrl?: string;
  createdAt: string; 
  updatedAt?: string; 
  likesCount: number; 
  commentsCount: number; 
  isLikedByCurrentUser: boolean; 
  timeAgo?: string;
  
  // Backend author fields
  authorName?: string;
  authorEmail?: string;
  authorUserName?: string;
}

export interface CreatePostRequest {
  userId: string;
  content: string;
  imageUrl?: string;
}

export interface UpdatePostRequest {
  content: string;
  imageUrl?: string;
}
