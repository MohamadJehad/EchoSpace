import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Post } from '../../interfaces';

@Component({
  selector: 'app-post-dropdown',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './post-dropdown.component.html',
  styleUrl: './post-dropdown.component.css'
})
export class PostDropdownComponent {
  @Input() post!: Post;
  @Input() currentUserId!: string;
  @Input() isOpen = false;
  
  @Output() editPost = new EventEmitter<Post>();
  @Output() deletePost = new EventEmitter<Post>();
  @Output() toggleDropdown = new EventEmitter<void>();

  get canEditOrDelete(): boolean {
    return this.post.userId === this.currentUserId;
  }

  onEdit(): void {
    this.editPost.emit(this.post);
    this.toggleDropdown.emit();
  }

  onDelete(): void {
    this.deletePost.emit(this.post);
    this.toggleDropdown.emit();
  }

  onToggleDropdown(event: Event): void {
    event.stopPropagation();
    this.toggleDropdown.emit();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdownElement = target.closest('.post-dropdown');
    
    if (!dropdownElement && this.isOpen) {
      this.toggleDropdown.emit();
    }
  }
}
