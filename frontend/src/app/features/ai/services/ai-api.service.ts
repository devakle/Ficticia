import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { NormalizeConditionResponseDto, RiskScoreResponseDto } from '../../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class AiApiService {
  private readonly http = inject(HttpClient);

  async normalizeCondition(apiBaseUrl: string, token: string, text: string): Promise<NormalizeConditionResponseDto> {
    const url = `${apiBaseUrl}/api/v1/ai/conditions/normalize`;
    return firstValueFrom(
      this.http.post<NormalizeConditionResponseDto>(url, { text }, { headers: this.authHeaders(token) })
    );
  }

  async scorePersonRisk(apiBaseUrl: string, token: string, personId: string): Promise<RiskScoreResponseDto> {
    const url = `${apiBaseUrl}/api/v1/ai/people/${personId}/risk-score`;
    return firstValueFrom(this.http.post<RiskScoreResponseDto>(url, {}, { headers: this.authHeaders(token) }));
  }

  private authHeaders(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
