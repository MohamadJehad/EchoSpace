import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FollowStatus {
  isFollowing: boolean;
}

export interface FollowCount {
  count: number;
}

@Injectable({
  providedIn: 'root'
})
export class FollowsService {
  private apiUrl = `${environment.apiUrl}/follows`;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Follow a user
  followUser(userId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${userId}`, {}, { headers: this.getHeaders() });
  }

  // Unfollow a user
  unfollowUser(userId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${userId}`, { headers: this.getHeaders() });
  }

  // Check if current user is following a user
  getFollowStatus(userId: string): Observable<FollowStatus> {
    return this.http.get<FollowStatus>(`${this.apiUrl}/${userId}/status`, { headers: this.getHeaders() });
  }

  // Get followers of a user
  getFollowers(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/followers`, { headers: this.getHeaders() });
  }

  // Get users that a user is following
  getFollowing(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/following`, { headers: this.getHeaders() });
  }

  // Get follower count
  getFollowerCount(userId: string): Observable<FollowCount> {
    return this.http.get<FollowCount>(`${this.apiUrl}/${userId}/followers/count`, { headers: this.getHeaders() });
  }

  // Get following count
  getFollowingCount(userId: string): Observable<FollowCount> {
    return this.http.get<FollowCount>(`${this.apiUrl}/${userId}/following/count`, { headers: this.getHeaders() });
  }
}

