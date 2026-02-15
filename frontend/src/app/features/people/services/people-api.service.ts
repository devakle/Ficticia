import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  PagedResult,
  PeopleSearchCriteria,
  PersonAttributeFormItemDto,
  PersonDto,
  UpsertAttributeValueDto
} from '../../../core/models/domain.models';

interface PagedResultRaw<T> {
  items?: T[];
  total?: number;
  page?: number;
  pageSize?: number;
  Items?: T[];
  Total?: number;
  Page?: number;
  PageSize?: number;
}

interface UpdatePersonPayload {
  id: string;
  fullName: string;
  identificationNumber: string;
  age: number;
  gender: number;
}

@Injectable({ providedIn: 'root' })
export class PeopleApiService {
  private readonly http = inject(HttpClient);

  async search(apiBaseUrl: string, token: string, criteria: PeopleSearchCriteria): Promise<PagedResult<PersonDto>> {
    let params = new HttpParams()
      .set('page', String(criteria.page))
      .set('pageSize', String(criteria.pageSize));

    if (criteria.name.trim()) {
      params = params.set('name', criteria.name.trim());
    }
    if (criteria.identificationNumber.trim()) {
      params = params.set('identificationNumber', criteria.identificationNumber.trim());
    }
    if (criteria.isActive) {
      params = params.set('isActive', criteria.isActive);
    }
    if (criteria.minAge !== null) {
      params = params.set('minAge', String(criteria.minAge));
    }
    if (criteria.maxAge !== null) {
      params = params.set('maxAge', String(criteria.maxAge));
    }

    for (const filter of criteria.dynamicFilters) {
      const key = filter.key.trim();
      const value = String(filter.value ?? '').trim();
      if (key && value) {
        params = params.set(`attr.${key}`, value);
      }
    }

    const url = `${apiBaseUrl}/api/v1/people`;
    const raw = await firstValueFrom(this.http.get<PagedResultRaw<PersonDto>>(url, { headers: this.authHeaders(token), params }));
    return this.normalizePagedResult(raw);
  }

  async create(apiBaseUrl: string, token: string, payload: Omit<PersonDto, 'id' | 'isActive'>): Promise<PersonDto> {
    const url = `${apiBaseUrl}/api/v1/people`;
    return firstValueFrom(this.http.post<PersonDto>(url, payload, { headers: this.authHeaders(token) }));
  }

  async update(apiBaseUrl: string, token: string, payload: UpdatePersonPayload): Promise<void> {
    const url = `${apiBaseUrl}/api/v1/people/${payload.id}`;
    await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders(token) }));
  }

  async setStatus(apiBaseUrl: string, token: string, id: string, isActive: boolean): Promise<void> {
    const url = `${apiBaseUrl}/api/v1/people/${id}/status`;
    await firstValueFrom(this.http.patch<void>(url, { id, isActive }, { headers: this.authHeaders(token) }));
  }

  async getAttributeForm(apiBaseUrl: string, token: string, personId: string, onlyActive: boolean): Promise<PersonAttributeFormItemDto[]> {
    const url = `${apiBaseUrl}/api/v1/people/${personId}/attributes/form`;
    const params = new HttpParams().set('onlyActive', String(onlyActive));
    return firstValueFrom(
      this.http.get<PersonAttributeFormItemDto[]>(url, { headers: this.authHeaders(token), params })
    );
  }

  async saveAttributes(apiBaseUrl: string, token: string, personId: string, payload: UpsertAttributeValueDto[]): Promise<void> {
    const url = `${apiBaseUrl}/api/v1/people/${personId}/attributes`;
    await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders(token) }));
  }

  private authHeaders(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  private normalizePagedResult<T>(raw: PagedResultRaw<T>): PagedResult<T> {
    return {
      items: raw.items ?? raw.Items ?? [],
      total: raw.total ?? raw.Total ?? 0,
      page: raw.page ?? raw.Page ?? 1,
      pageSize: raw.pageSize ?? raw.PageSize ?? 20
    };
  }
}
