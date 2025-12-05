import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Comment, CreateCommentRequest, UpdateCommentRequest } from '../interfaces/comment.interface';

@Injectable({
  providedIn: 'root'
})
export class CommentsService {
  private apiUrl = `${environment.apiUrl}/comments`;

  constructor(private http: HttpClient) {}

  // Get all comments
  getAllComments(): Observable<Comment[]> {
    return this.http.get<Comment[]>(this.apiUrl);
  }

  // Get comment by ID
  getCommentById(commentId: string): Observable<Comment> {
    return this.http.get<Comment>(`${this.apiUrl}/${commentId}`);
  }

  // Get comments by post ID
  getCommentsByPost(postId: string): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.apiUrl}/post/${postId}`);
  }

  // Get comments by user ID
  getCommentsByUser(userId: string): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.apiUrl}/user/${userId}`);
  }

  // Get comment count for a post
  getCommentCountByPost(postId: string): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/post/${postId}/count`);
  }

  // Create a new comment
  createComment(request: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(this.apiUrl, request);
  }

  // Update a comment
  updateComment(commentId: string, request: UpdateCommentRequest): Observable<Comment> {
    return this.http.put<Comment>(`${this.apiUrl}/${commentId}`, request);
  }

  // Delete a comment
  deleteComment(commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${commentId}`);
  }
}

