import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  // Follow a user
  followUser(userId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${userId}`, {});
  }

  // Unfollow a user
  unfollowUser(userId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${userId}`);
  }

  // Check if current user is following a user
  getFollowStatus(userId: string): Observable<FollowStatus> {
    return this.http.get<FollowStatus>(`${this.apiUrl}/${userId}/status`);
  }

  // Get followers of a user
  getFollowers(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/followers`);
  }

  // Get users that a user is following
  getFollowing(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/following`);
  }

  // Get follower count
  getFollowerCount(userId: string): Observable<FollowCount> {
    return this.http.get<FollowCount>(`${this.apiUrl}/${userId}/followers/count`);
  }

  // Get following count
  getFollowingCount(userId: string): Observable<FollowCount> {
    return this.http.get<FollowCount>(`${this.apiUrl}/${userId}/following/count`);
  }
}

