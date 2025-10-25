import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { SearchService, SearchResult } from '../../services/search.service';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './search-bar.component.html',
  styleUrl: './search-bar.component.css'
})
export class SearchBarComponent {
  searchQuery = '';
  searchResults: SearchResult[] = [];
  isSearching = false;
  showDropdown = false;
  private searchSubject = new Subject<string>();

  constructor(
    private searchService: SearchService,
    private router: Router
  ) {
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(300), // Wait 300ms after user stops typing
      distinctUntilChanged(), // Only search if query changed
      switchMap(query => {
        if (query && query.trim().length > 0) {
          this.isSearching = true;
          return this.searchService.searchUsers(query, 5);
        } else {
          this.isSearching = false;
          return [];
        }
      })
    ).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.isSearching = false;
        this.showDropdown = true;
      },
      error: (error) => {
        console.error('Search error:', error);
        this.isSearching = false;
        this.searchResults = [];
      }
    });
  }

  onSearchInput(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onFocus(): void {
    if (this.searchQuery && this.searchResults.length > 0) {
      this.showDropdown = true;
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchResults = [];
    this.showDropdown = false;
  }

  seeAllResults(): void {
    this.router.navigate(['/search'], {
      queryParams: { q: this.searchQuery }
    });
    this.showDropdown = false;
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    
    const words = name.trim().split(' ');
    if (words.length === 1) {
      return words[0].substring(0, 2).toUpperCase();
    }
    
    return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.relative')) {
      this.showDropdown = false;
    }
  }
}
