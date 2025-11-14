import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { Post, CreatePostRequest, UpdatePostRequest } from '../interfaces';

@Injectable({
  providedIn: 'root'
})
export class PostsService {
  private apiUrl = `${environment.apiUrl}/posts`;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Get all posts
  getAllPosts(): Observable<Post[]> {
    return this.http.get<Post[]>(this.apiUrl, { headers: this.getHeaders() });
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
    return this.http.post<Post>(this.apiUrl, request, { headers: this.getHeaders() });
  }

  // Update post
  updatePost(id: string, request: UpdatePostRequest, userId: string): Observable<Post> {
    return this.http.put<Post>(`${this.apiUrl}/${id}?userId=${userId}`, request, { headers: this.getHeaders() });
  }

  // Delete post
  deletePost(id: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}?userId=${userId}`, { headers: this.getHeaders() });
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
    return this.http.get<Post[]>(`${this.apiUrl}/following`, { headers: this.getHeaders() });
  }
}
