import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface LoginResult {
  accessToken: string;
  roles: string[];
}

interface LoginResponse {
  access_token: string;
  roles?: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  async login(apiBaseUrl: string, email: string, password: string): Promise<LoginResult> {
    const url = `${apiBaseUrl}/api/v1/auth/login`;
    const body = { email, password };
    const res = await firstValueFrom(this.http.post<LoginResponse>(url, body));
    return {
      accessToken: res.access_token,
      roles: Array.isArray(res.roles) ? res.roles : []
    };
  }
}
