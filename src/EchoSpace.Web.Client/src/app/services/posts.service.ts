import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { Post, CreatePostRequest, UpdatePostRequest, ReportedPost } from '../interfaces';

@Injectable({
  providedIn: 'root'
})
export class PostsService {
  private apiUrl = `${environment.apiUrl}/posts`;

  constructor(private http: HttpClient) {}

  // Get all posts
  getAllPosts(): Observable<Post[]> {
    return this.http.get<Post[]>(this.apiUrl);
  }

  // Get post by ID
  getPostById(id: string): Observable<Post> {
    return this.http.get<Post>(`${this.apiUrl}/${id}`);
  }

  // Get posts by user ID
  getPostsByUser(userId: string): Observable<Post[]> {
    return this.http.get<Post[]>(`${this.apiUrl}/user/${userId}`);
  }

  // Get recent posts
  getRecentPosts(count: number = 10): Observable<Post[]> {
    return this.http.get<Post[]>(`${this.apiUrl}/recent?count=${count}`);
  }

  // Create new post
  createPost(request: CreatePostRequest): Observable<Post> {
    return this.http.post<Post>(this.apiUrl, request);
  }

  // Update post
  updatePost(id: string, request: UpdatePostRequest, userId: string): Observable<Post> {
    return this.http.put<Post>(`${this.apiUrl}/${id}?userId=${userId}`, request);
  }

  // Delete post
  deletePost(id: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}?userId=${userId}`);
  }

  // Check if post exists
  postExists(id: string): Observable<boolean> {
    return this.http.head(`${this.apiUrl}/${id}`, { observe: 'response' })
      .pipe(
        map(response => response.status === 200)
      );
  }

  // Get posts from following users
  getPostsFromFollowing(): Observable<Post[]> {
    return this.http.get<Post[]>(`${this.apiUrl}/following`);
  }

  // Report a post
  reportPost(postId: string, reason?: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${postId}/report`,
      { reason }
    );
  }

  // Get reported posts (Operation/Admin only)
  getReportedPosts(): Observable<ReportedPost[]> {
    return this.http.get<ReportedPost[]>(`${this.apiUrl}/reported`);
  }

  // Translate a post
  translatePost(postId: string, language: string = 'en'): Observable<{ postId: string; language: string; translated: string }> {
    return this.http.post<{ postId: string; language: string; translated: string }>(
      `${this.apiUrl}/translate`,
      { postId, language }
    );
  }

  // Summarize a post
  summarizePost(postId: string): Observable<{ postId: string; summary: string }> {
    return this.http.post<{ postId: string; summary: string }>(
      `${this.apiUrl}/summarize`,
      { postId }
    );
  }

  // Get posts by tag ID
  getPostsByTag(tagId: string): Observable<Post[]> {
    return this.http.get<Post[]>(`${this.apiUrl}/tag/${tagId}`);
  }
}
