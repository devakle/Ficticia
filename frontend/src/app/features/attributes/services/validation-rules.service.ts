import { Injectable } from '@angular/core';
import { AttributeDataType, ValidationRulesDraft } from '../../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class ValidationRulesService {
  emptyDraft(): ValidationRulesDraft {
    return {
      required: false,
      maxLength: null,
      regex: '',
      min: null,
      max: null,
      minDate: '',
      maxDate: '',
      allowedValuesText: ''
    };
  }

  parseDraft(dataType: AttributeDataType, rawJson: string | null): ValidationRulesDraft {
    const draft = this.emptyDraft();
    const parsed = this.tryParseRules(rawJson);
    if (!parsed) {
      return draft;
    }

    const required = parsed['required'] ?? parsed['Required'];
    draft.required = typeof required === 'boolean' ? required : false;

    if (dataType === 2) {
      const maxLength = parsed['maxLength'] ?? parsed['MaxLength'];
      draft.maxLength = typeof maxLength === 'number' ? maxLength : null;
      const regex = parsed['regex'] ?? parsed['Regex'];
      draft.regex = typeof regex === 'string' ? regex : '';
    }

    if (dataType === 3) {
      const min = parsed['min'] ?? parsed['Min'];
      const max = parsed['max'] ?? parsed['Max'];
      draft.min = typeof min === 'number' ? min : null;
      draft.max = typeof max === 'number' ? max : null;
    }

    if (dataType === 4) {
      const minDate = parsed['minDate'] ?? parsed['MinDate'];
      const maxDate = parsed['maxDate'] ?? parsed['MaxDate'];
      draft.minDate = this.normalizeDateInput(minDate);
      draft.maxDate = this.normalizeDateInput(maxDate);
    }

    if (dataType === 5) {
      const allowed = parsed['allowedValues'] ?? parsed['AllowedValues'];
      if (Array.isArray(allowed)) {
        draft.allowedValuesText = allowed
          .filter((x): x is string => typeof x === 'string')
          .map(x => x.trim())
          .filter(x => x.length > 0)
          .join(', ');
      }
    }

    return draft;
  }

  buildJson(dataType: AttributeDataType, draft: ValidationRulesDraft): string | null {
    const rules: Record<string, unknown> = {};

    if (draft.required) {
      rules['required'] = true;
    }

    if (dataType === 2) {
      if (draft.maxLength !== null && Number.isFinite(draft.maxLength)) {
        rules['maxLength'] = draft.maxLength;
      }
      const regex = draft.regex.trim();
      if (regex) {
        rules['regex'] = regex;
      }
    }

    if (dataType === 3) {
      if (draft.min !== null && Number.isFinite(draft.min)) {
        rules['min'] = draft.min;
      }
      if (draft.max !== null && Number.isFinite(draft.max)) {
        rules['max'] = draft.max;
      }
    }

    if (dataType === 4) {
      const minDate = draft.minDate.trim();
      const maxDate = draft.maxDate.trim();
      if (minDate) {
        rules['minDate'] = minDate;
      }
      if (maxDate) {
        rules['maxDate'] = maxDate;
      }
    }

    if (dataType === 5) {
      const allowedValues = this.parseAllowedValues(draft.allowedValuesText);
      if (allowedValues.length) {
        rules['allowedValues'] = allowedValues;
      }
    }

    return Object.keys(rules).length ? JSON.stringify(rules) : null;
  }

  preview(dataType: AttributeDataType, draft: ValidationRulesDraft): string {
    const json = this.buildJson(dataType, draft);
    return json ?? '(sin reglas)';
  }

  allowedValues(rawJson: string | null): string[] {
    const parsed = this.tryParseRules(rawJson);
    if (!parsed) {
      return [];
    }

    const rawAllowed = parsed['allowedValues'] ?? parsed['AllowedValues'];
    if (!Array.isArray(rawAllowed)) {
      return [];
    }

    return rawAllowed
      .filter((x): x is string => typeof x === 'string')
      .map(x => x.trim())
      .filter(x => x.length > 0);
  }

  private parseAllowedValues(source: string): string[] {
    return source
      .split(/[\n,;]+/)
      .map(x => x.trim())
      .filter(x => x.length > 0);
  }

  private normalizeDateInput(value: unknown): string {
    if (typeof value !== 'string' || !value.trim()) {
      return '';
    }

    const raw = value.trim();
    return raw.length >= 10 ? raw.slice(0, 10) : raw;
  }

  private tryParseRules(rawJson: string | null): Record<string, unknown> | null {
    if (!rawJson || !rawJson.trim()) {
      return null;
    }

    try {
      const parsed = JSON.parse(rawJson);
      return parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : null;
    } catch {
      return null;
    }
  }
}
