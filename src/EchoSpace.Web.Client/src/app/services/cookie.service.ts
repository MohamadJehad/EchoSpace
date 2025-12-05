import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class CookieService {
  /**
   * Set a cookie
   * @param name Cookie name
   * @param value Cookie value
   * @param days Expiration in days (default: 7)
   */
  set(name: string, value: string, days: number = 7): void {
    const expires = new Date();
    expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
    
    // Set cookie with Secure and SameSite attributes
    // Note: Secure requires HTTPS, so we'll set it conditionally
    const isSecure = location.protocol === 'https:';
    const secureFlag = isSecure ? '; Secure' : '';
    
    document.cookie = `${name}=${value}; expires=${expires.toUTCString()}; path=/; SameSite=Strict${secureFlag}`;
  }

  /**
   * Get a cookie value
   * @param name Cookie name
   * @returns Cookie value or null
   */
  get(name: string): string | null {
    const nameEQ = name + '=';
    const cookies = document.cookie.split(';');
    
    for (let i = 0; i < cookies.length; i++) {
      let cookie = cookies[i];
      while (cookie.charAt(0) === ' ') {
        cookie = cookie.substring(1, cookie.length);
      }
      if (cookie.indexOf(nameEQ) === 0) {
        return cookie.substring(nameEQ.length, cookie.length);
      }
    }
    return null;
  }

  /**
   * Delete a cookie
   * @param name Cookie name
   */
  delete(name: string): void {
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
  }

  /**
   * Check if a cookie exists
   * @param name Cookie name
   * @returns True if cookie exists
   */
  has(name: string): boolean {
    return this.get(name) !== null;
  }
}

