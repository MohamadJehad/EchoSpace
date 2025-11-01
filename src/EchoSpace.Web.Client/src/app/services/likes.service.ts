import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LikeResponse {
  success: boolean;
  message?: string;
  isLiked?: boolean;
  likesCount: number;
}

export interface LikeStatus {
  isLiked: boolean;
  likesCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class LikesService {
  private apiUrl = `${environment.apiUrl}/likes`;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Like a post
  likePost(postId: string): Observable<LikeResponse> {
    return this.http.post<LikeResponse>(`${this.apiUrl}/${postId}`, {}, { headers: this.getHeaders() });
  }

  // Unlike a post
  unlikePost(postId: string): Observable<LikeResponse> {
    return this.http.delete<LikeResponse>(`${this.apiUrl}/${postId}`, { headers: this.getHeaders() });
  }

  // Toggle like status (like if not liked, unlike if liked)
  toggleLike(postId: string): Observable<LikeResponse> {
    return this.http.post<LikeResponse>(`${this.apiUrl}/${postId}/toggle`, {}, { headers: this.getHeaders() });
  }

  // Get like status for a post
  getLikeStatus(postId: string): Observable<LikeStatus> {
    return this.http.get<LikeStatus>(`${this.apiUrl}/${postId}/status`, { headers: this.getHeaders() });
  }
}

