export interface SuggestedUser {
  id: string;
  name: string;
  username: string;
  email: string;
  initials: string;
  postsCount: number;
  createdAt: string;
  mutualFriends?: number; // Optional for future use
}
