export interface Comment {
  commentId: string;
  postId: string;
  userId: string;
  content: string;
  createdAt: string;
  userName: string;
  userEmail: string;
  timeAgo?: string;
}

export interface CreateCommentRequest {
  postId: string;
  userId: string;
  content: string;
}

export interface UpdateCommentRequest {
  content: string;
}

