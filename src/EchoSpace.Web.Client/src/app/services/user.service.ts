import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface User {
  id: string;
  name: string;
  email: string;
  createdAt: string;
  updatedAt?: string;
  profilePhotoId?: string | null;
  lockoutEnabled?: boolean;
  lockoutEnd?: string | null;
  accessFailedCount?: number;
  role?: number | string; // Can be number (enum) or string
}

export interface CreateUserRequest {
  name: string;
  email: string;
}

export interface UpdateUserRequest {
  name?: string;
  email?: string;
  role?: number; // UserRole enum value (0=User, 1=Admin, 2=Moderator, 3=Operation)
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) { }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl);
  }

  getUserById(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  createUser(user: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.apiUrl, user);
  }

  updateUser(id: string, user: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/${id}`, user);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  uploadProfilePhoto(file: File): Observable<{ message: string; imageId: string; imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ message: string; imageId: string; imageUrl: string }>(
      `${this.apiUrl}/me/profile-photo`, 
      formData
    );
  }

  removeProfilePhoto(): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/me/profile-photo`);
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`);
  }

  lockUser(id: string): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/${id}/lock`, {});
  }

  unlockUser(id: string): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/${id}/unlock`, {});
  }
}

