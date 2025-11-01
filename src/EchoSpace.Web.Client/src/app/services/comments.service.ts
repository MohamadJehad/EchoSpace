import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Comment, CreateCommentRequest, UpdateCommentRequest } from '../interfaces/comment.interface';

@Injectable({
  providedIn: 'root'
})
export class CommentsService {
  private apiUrl = `${environment.apiUrl}/comments`;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Get all comments
  getAllComments(): Observable<Comment[]> {
    return this.http.get<Comment[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  // Get comment by ID
  getCommentById(commentId: string): Observable<Comment> {
    return this.http.get<Comment>(`${this.apiUrl}/${commentId}`, { headers: this.getHeaders() });
  }

  // Get comments by post ID
  getCommentsByPost(postId: string): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.apiUrl}/post/${postId}`, { headers: this.getHeaders() });
  }

  // Get comments by user ID
  getCommentsByUser(userId: string): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.apiUrl}/user/${userId}`, { headers: this.getHeaders() });
  }

  // Get comment count for a post
  getCommentCountByPost(postId: string): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/post/${postId}/count`, { headers: this.getHeaders() });
  }

  // Create a new comment
  createComment(request: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(this.apiUrl, request, { headers: this.getHeaders() });
  }

  // Update a comment
  updateComment(commentId: string, request: UpdateCommentRequest): Observable<Comment> {
    return this.http.put<Comment>(`${this.apiUrl}/${commentId}`, request, { headers: this.getHeaders() });
  }

  // Delete a comment
  deleteComment(commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${commentId}`, { headers: this.getHeaders() });
  }
}

