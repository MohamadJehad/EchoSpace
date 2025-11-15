export interface Post {
  postId: string; 
  userId: string; 
  author?: {
    name: string;
    initials: string;
    userId?: string;
    profilePhotoUrl?: string | null;
  };
  content: string;
  imageUrl?: string;
  createdAt: string; 
  updatedAt?: string; 
  likesCount: number; 
  commentsCount: number; 
  isLikedByCurrentUser: boolean; 
  isFollowingAuthor?: boolean;
  timeAgo?: string;
  
  // Backend author fields
  authorName?: string;
  authorEmail?: string;
  authorUserName?: string;
  authorProfilePhotoId?: string;
  
  // Tag information
  tags?: Array<{
    tagId: string;
    name: string;
    color?: string;
  }>;
}

export interface CreatePostRequest {
  userId: string;
  content: string;
  imageUrl?: string;
  tagIds?: string[];
}

export interface UpdatePostRequest {
  content: string;
  imageUrl?: string;
}

export interface ReportPostRequest {
  reason?: string;
}

export interface ReportedPost {
  postId: string;
  userId: string;
  content: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt?: string;
  reportCount: number;
  likesCount: number;
  commentsCount: number;
  authorName: string;
  authorEmail: string;
  authorUserName: string;
  reports: ReportInfo[];
}

export interface ReportInfo {
  reportId: string;
  userId: string;
  reason?: string;
  createdAt: string;
  reporterName: string;
  reporterEmail: string;
}