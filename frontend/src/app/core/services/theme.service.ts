import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'admin_theme_mode';

  getInitialTheme(): 'light' | 'dark' {
    const stored = localStorage.getItem(this.storageKey);
    if (stored === 'dark' || stored === 'light') {
      return stored;
    }

    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  toggle(themeMode: 'light' | 'dark'): 'light' | 'dark' {
    const next = themeMode === 'dark' ? 'light' : 'dark';
    localStorage.setItem(this.storageKey, next);
    return next;
  }
}
