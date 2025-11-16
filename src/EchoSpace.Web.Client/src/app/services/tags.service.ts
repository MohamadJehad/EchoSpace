import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Tag } from '../interfaces/tag.interface';

export interface TrendingTag {
  tagId: string;
  name: string;
  description?: string;
  color?: string;
  postsCount: number;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class TagsService {
  private apiUrl = `${environment.apiUrl}/tags`;

  constructor(private http: HttpClient) {}

  // Get all tags
  getAllTags(): Observable<Tag[]> {
    return this.http.get<Tag[]>(this.apiUrl);
  }

  // Get tag by ID
  getTagById(id: string): Observable<Tag> {
    return this.http.get<Tag>(`${this.apiUrl}/${id}`);
  }

  // Get trending tags
  getTrendingTags(count: number = 10): Observable<TrendingTag[]> {
    return this.http.get<TrendingTag[]>(`${this.apiUrl}/trending?count=${count}`);
  }
}

