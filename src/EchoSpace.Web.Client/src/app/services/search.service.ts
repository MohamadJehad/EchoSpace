import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SearchResult {
  id: string;
  name: string;
  userName: string;
  email: string;
  profileImageUrl?: string;
  matchScore: number;
}

@Injectable({
  providedIn: 'root'
})
export class SearchService {
  private apiUrl = `${environment.apiUrl}/search`;

  constructor(private http: HttpClient) { }

  searchUsers(query: string, limit: number = 10): Observable<SearchResult[]> {
    return this.http.get<SearchResult[]>(`${this.apiUrl}/users`, {
      params: {
        q: query,
        limit: limit.toString()
      }
    });
  }
}
