import { Injectable } from '@angular/core';

type RiskBand = 'low' | 'medium' | 'high' | 'unknown';

@Injectable({ providedIn: 'root' })
export class RiskBandService {
  isBand(band: string | number, expected: 'low' | 'medium' | 'high'): boolean {
    return this.normalize(band) === expected;
  }

  label(band: string | number): string {
    const normalized = this.normalize(band);
    if (normalized === 'low') {
      return 'Riesgo bajo';
    }
    if (normalized === 'medium') {
      return 'Riesgo medio';
    }
    if (normalized === 'high') {
      return 'Riesgo alto';
    }

    return `Banda ${String(band)}`;
  }

  private normalize(band: string | number): RiskBand {
    if (band === 1 || band === '1') {
      return 'low';
    }
    if (band === 2 || band === '2') {
      return 'medium';
    }
    if (band === 3 || band === '3') {
      return 'high';
    }

    const normalized = String(band).trim().toLowerCase();
    if (normalized === 'low') {
      return 'low';
    }
    if (normalized === 'medium') {
      return 'medium';
    }
    if (normalized === 'high') {
      return 'high';
    }

    return 'unknown';
  }
}
