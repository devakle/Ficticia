import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  async login(apiBaseUrl: string, email: string, password: string): Promise<string> {
    const url = `${apiBaseUrl}/api/v1/auth/login`;
    const body = { email, password };
    const res = await firstValueFrom(this.http.post<{ access_token: string }>(url, body));
    return res.access_token;
  }
}
