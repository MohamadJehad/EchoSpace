import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  // Like a post
  likePost(postId: string): Observable<LikeResponse> {
    return this.http.post<LikeResponse>(`${this.apiUrl}/${postId}`, {});
  }

  // Unlike a post
  unlikePost(postId: string): Observable<LikeResponse> {
    return this.http.delete<LikeResponse>(`${this.apiUrl}/${postId}`);
  }

  // Toggle like status (like if not liked, unlike if liked)
  toggleLike(postId: string): Observable<LikeResponse> {
    return this.http.post<LikeResponse>(`${this.apiUrl}/${postId}/toggle`, {});
  }

  // Get like status for a post
  getLikeStatus(postId: string): Observable<LikeStatus> {
    return this.http.get<LikeStatus>(`${this.apiUrl}/${postId}/status`);
  }
}

