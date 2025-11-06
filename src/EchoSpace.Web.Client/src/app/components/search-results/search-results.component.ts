import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { SearchService, SearchResult } from '../../services/search.service';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './search-results.component.html',
  styleUrl: './search-results.component.css'
})
export class SearchResultsComponent implements OnInit {
  searchQuery = '';
  results: SearchResult[] = [];
  isLoading = false;
  private searchSubject = new Subject<string>();

  constructor(
    private searchService: SearchService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(query => {
        if (query && query.trim().length > 0) {
          this.isLoading = true;
          return this.searchService.searchUsers(query, 50);
        } else {
          this.isLoading = false;
          return [];
        }
      })
    ).subscribe({
      next: (results) => {
        this.results = results;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Search error:', error);
        this.isLoading = false;
        this.results = [];
      }
    });
  }

  ngOnInit(): void {
    // Get query from URL params
    this.route.queryParams.subscribe(params => {
      const query = params['q'];
      if (query) {
        this.searchQuery = query;
        this.performSearch();
      }
    });
  }

  performSearch(): void {
    this.searchSubject.next(this.searchQuery);
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    
    const words = name.trim().split(' ');
    if (words.length === 1) {
      return words[0].substring(0, 2).toUpperCase();
    }
    
    return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
  }

  clearAndGoHome(): void {
    this.router.navigate(['/home']);
  }
}
