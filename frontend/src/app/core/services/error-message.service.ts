import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ErrorMessageService {
  toMessage(err: unknown): string {
    const status = (err as { status?: number }).status;
    const body = (err as { error?: unknown }).error;

    if (typeof body === 'string') {
      return status ? `Solicitud fallida (${status}): ${body}` : body;
    }

    if (body && typeof body === 'object') {
      const asRecord = body as Record<string, unknown>;
      const detail = asRecord['message'] ?? asRecord['title'] ?? JSON.stringify(body);
      return status ? `Solicitud fallida (${status}): ${detail}` : String(detail);
    }

    return status ? `Solicitud fallida (${status}).` : 'Error inesperado.';
  }
}
