export interface Post {
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
