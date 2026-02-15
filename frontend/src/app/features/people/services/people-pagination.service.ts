import { Injectable } from '@angular/core';

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
}
