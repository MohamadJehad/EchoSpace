import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Tag } from '../interfaces/tag.interface';

@Injectable({
  providedIn: 'root'
})
export class TagsService {
  private apiUrl = `${environment.apiUrl}/tags`;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Get all tags
  getAllTags(): Observable<Tag[]> {
    return this.http.get<Tag[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  // Get tag by ID
  getTagById(id: string): Observable<Tag> {
    return this.http.get<Tag>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }
}

