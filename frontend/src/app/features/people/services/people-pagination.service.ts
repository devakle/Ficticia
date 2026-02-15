import { Injectable } from '@angular/core';

export type PaginationItem = number | 'ellipsis';

@Injectable({ providedIn: 'root' })
export class PeoplePaginationService {
  normalizePage(page: number): number {
    if (!Number.isFinite(page)) {
      return 1;
    }

    return Math.max(1, Math.trunc(page));
  }

  normalizePageSize(pageSize: number): number {
    if (!Number.isFinite(pageSize)) {
      return 20;
    }

    return Math.min(100, Math.max(1, Math.trunc(pageSize)));
  }

  totalPages(total: number, pageSize: number): number {
    return Math.max(1, Math.ceil(total / pageSize));
  }

  clampPage(page: number, total: number, pageSize: number): number {
    const normalized = this.normalizePage(page);
    return Math.min(this.totalPages(total, pageSize), normalized);
  }

  visibleRange(total: number, page: number, pageSize: number, itemCount: number): { from: number; to: number } {
    if (total === 0 || itemCount === 0) {
      return { from: 0, to: 0 };
    }

    const from = (page - 1) * pageSize + 1;
    const to = Math.min(total, from + itemCount - 1);
    return { from, to };
  }

  visiblePages(page: number, totalPages: number, maxVisiblePages = 3): PaginationItem[] {
    const safeTotalPages = Math.max(1, Math.trunc(totalPages));
    const safePage = Math.min(Math.max(1, Math.trunc(page)), safeTotalPages);
    const visibleCount = Math.min(Math.max(1, Math.trunc(maxVisiblePages)), safeTotalPages);

    if (safeTotalPages <= visibleCount) {
      return Array.from({ length: safeTotalPages }, (_, index) => index + 1);
    }

    const left = Math.floor((visibleCount - 1) / 2);
    const right = visibleCount - left - 1;
    let start = safePage - left;
    let end = safePage + right;

    if (start < 1) {
      end += 1 - start;
      start = 1;
    }
    if (end > safeTotalPages) {
      start -= end - safeTotalPages;
      end = safeTotalPages;
    }

    const pages: PaginationItem[] = [];

    if (start > 1) {
      pages.push('ellipsis');
    }
    for (let current = start; current <= end; current += 1) {
      pages.push(current);
    }
    if (end < safeTotalPages) {
      pages.push('ellipsis');
    }

    return pages;
  }
}
