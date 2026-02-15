import { Injectable, signal } from '@angular/core';
import { ToastMessage } from '../models/ui.models';

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly notifications = signal<ToastMessage[]>([]);
  private readonly minDurationMs = 6000;
  private readonly maxDurationMs = 15000;
  private readonly readingMsPerWord = 333;
  private readonly baseMs = 2000;

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  dismiss(id: number): void {
    this.notifications.update(items => items.filter(n => n.id !== id));
  }

  private push(type: 'success' | 'error', text: string): void {
    const id = Date.now() + Math.floor(Math.random() * 1000);
    this.notifications.update(items => [...items, { id, type, text }]);
    setTimeout(() => this.dismiss(id), this.displayDuration(text));
  }

  private displayDuration(text: string): number {
    const words = text.trim().split(/\s+/).filter(Boolean).length;
    const estimated = this.baseMs + words * this.readingMsPerWord;
    return Math.min(this.maxDurationMs, Math.max(this.minDurationMs, estimated));
  }
}
