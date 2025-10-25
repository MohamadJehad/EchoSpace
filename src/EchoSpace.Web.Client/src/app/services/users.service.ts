import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SuggestedUser } from '../interfaces';

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  // Get suggested users
  getSuggestedUsers(count: number = 10): Observable<SuggestedUser[]> {
    return this.http.get<SuggestedUser[]>(`${this.apiUrl}/suggested?count=${count}`);
  }

  // Get user by ID
  getUserById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }
}
