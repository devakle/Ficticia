import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AttributeDefinitionDto } from '../../../core/models/domain.models';

interface AttributeDefinitionPayload {
  key: string;
  displayName: string;
  dataType: number;
  isFilterable: boolean;
  validationRulesJson: string | null;
}

interface UpdateAttributeDefinitionPayload {
  id: string;
  displayName: string;
  isFilterable: boolean;
  isActive: boolean;
  validationRulesJson: string | null;
}

@Injectable({ providedIn: 'root' })
export class AttributesApiService {
  private readonly http = inject(HttpClient);

  async getDefinitions(apiBaseUrl: string, token: string, onlyActive: boolean): Promise<AttributeDefinitionDto[]> {
    const url = `${apiBaseUrl}/api/v1/attributes/definitions`;
    const params = new HttpParams().set('onlyActive', String(onlyActive));
    return firstValueFrom(this.http.get<AttributeDefinitionDto[]>(url, { headers: this.authHeaders(token), params }));
  }

  async createDefinition(apiBaseUrl: string, token: string, payload: AttributeDefinitionPayload): Promise<AttributeDefinitionDto> {
    const url = `${apiBaseUrl}/api/v1/attributes/definitions`;
    return firstValueFrom(this.http.post<AttributeDefinitionDto>(url, payload, { headers: this.authHeaders(token) }));
  }

  async updateDefinition(apiBaseUrl: string, token: string, definitionId: string, payload: UpdateAttributeDefinitionPayload): Promise<void> {
    const url = `${apiBaseUrl}/api/v1/attributes/definitions/${definitionId}`;
    await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders(token) }));
  }

  private authHeaders(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
