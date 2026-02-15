import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, HostBinding, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type Gender = 0 | 1 | 2 | 3;
type AttributeDataType = 1 | 2 | 3 | 4 | 5;

interface PersonDto {
  id: string;
  fullName: string;
  identificationNumber: string;
  age: number;
  gender: Gender;
  isActive: boolean;
}

interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

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

interface AttributeDefinitionDto {
  id: string;
  key: string;
  displayName: string;
  dataType: AttributeDataType;
  isFilterable: boolean;
  isActive: boolean;
  validationRulesJson: string | null;
}

interface PersonAttributeFormItemDto {
  key: string;
  displayName: string;
  dataType: AttributeDataType;
  isFilterable: boolean;
  isActive: boolean;
  validationRulesJson: string | null;
  boolValue: boolean | null;
  stringValue: string | null;
  numberValue: number | null;
  dateValue: string | null;
  updatedAt: string | null;
}

interface UpsertAttributeValueDto {
  key: string;
  boolValue: boolean | null;
  stringValue: string | null;
  numberValue: number | null;
  dateValue: string | null;
}

interface NormalizeConditionResponseDto {
  code: string;
  label: string;
  confidence: number;
  matchedTerms: string[];
  suggestedAttributes: UpsertAttributeValueDto[];
  source: string;
}

interface RiskScoreResponseDto {
  score: number;
  band: string | number;
  reasons: string[] | null | undefined;
}

interface ToastMessage {
  id: number;
  type: 'success' | 'error';
  text: string;
}

interface DynamicFilterInput {
  key: string;
  value: string | number | null;
}

interface ValidationRulesDraft {
  required: boolean;
  maxLength: number | null;
  regex: string;
  min: number | null;
  max: number | null;
  minDate: string;
  maxDate: string;
  allowedValuesText: string;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly themeStorageKey = 'admin_theme_mode';

  readonly genders = [
    { value: 0, label: 'Desconocido' },
    { value: 1, label: 'Masculino' },
    { value: 2, label: 'Femenino' },
    { value: 3, label: 'Otro' }
  ];

  readonly attributeTypes = [
    { value: 1, label: 'Booleano' },
    { value: 2, label: 'Texto' },
    { value: 3, label: 'Numero' },
    { value: 4, label: 'Fecha' },
    { value: 5, label: 'Enumerado' }
  ];

  apiBaseUrl = '';
  email = 'admin@ficticia.local';
  password = 'Admin123!';
  token = localStorage.getItem('admin_token') ?? '';
  themeMode: 'light' | 'dark' = this.getInitialTheme();

  busy = false;
  notifications = signal<ToastMessage[]>([]);

  includeInactiveDefinitions = false;

  peopleSearch = {
    name: '',
    identificationNumber: '',
    isActive: '',
    minAge: null as number | null,
    maxAge: null as number | null,
    page: 1,
    pageSize: 20,
    dynamicFilters: [{ key: '', value: '' }] as DynamicFilterInput[]
  };

  people: PersonDto[] = [];
  totalPeople = 0;
  selectedPerson: PersonDto | null = null;

  personForm = {
    fullName: '',
    identificationNumber: '',
    age: 18,
    gender: 0 as Gender
  };

  definitions: AttributeDefinitionDto[] = [];
  newDefinition = {
    key: '',
    displayName: '',
    dataType: 1 as AttributeDataType,
    isFilterable: true
  };
  newDefinitionRules: ValidationRulesDraft = this.emptyRulesDraft();
  definitionRulesDrafts: Record<string, ValidationRulesDraft> = {};

  personAttributes: PersonAttributeFormItemDto[] = [];
  conditionText = '';
  normalizedCondition: NormalizeConditionResponseDto | null = null;
  riskScore: RiskScoreResponseDto | null = null;

  @HostBinding('class.dark-theme')
  get isDarkTheme(): boolean {
    return this.themeMode === 'dark';
  }

  async login(): Promise<void> {
    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/auth/login`;
      const body = { email: this.email, password: this.password };
      const res = await firstValueFrom(this.http.post<{ access_token: string }>(url, body));

      this.token = res.access_token;
      localStorage.setItem('admin_token', this.token);
      this.notifySuccess('Autenticacion exitosa.');

      await Promise.all([this.searchPeople(), this.loadDefinitions()]);
    });
  }

  logout(): void {
    this.token = '';
    localStorage.removeItem('admin_token');
    this.notifySuccess('Token limpiado.');
  }

  async searchPeople(): Promise<void> {
    await this.run(async () => {
      let params = new HttpParams()
        .set('page', String(this.peopleSearch.page))
        .set('pageSize', String(this.peopleSearch.pageSize));

      if (this.peopleSearch.name.trim()) {
        params = params.set('name', this.peopleSearch.name.trim());
      }
      if (this.peopleSearch.identificationNumber.trim()) {
        params = params.set('identificationNumber', this.peopleSearch.identificationNumber.trim());
      }
      if (this.peopleSearch.isActive) {
        params = params.set('isActive', this.peopleSearch.isActive);
      }
      if (this.peopleSearch.minAge !== null) {
        params = params.set('minAge', String(this.peopleSearch.minAge));
      }
      if (this.peopleSearch.maxAge !== null) {
        params = params.set('maxAge', String(this.peopleSearch.maxAge));
      }

      for (const filter of this.peopleSearch.dynamicFilters) {
        const key = filter.key.trim();
        const value = String(filter.value ?? '').trim();
        if (key && value) {
          params = params.set(`attr.${key}`, value);
        }
      }

      const url = `${this.apiBaseUrl}/api/v1/people`;
      const raw = await firstValueFrom(this.http.get<PagedResultRaw<PersonDto>>(url, { headers: this.authHeaders, params }));
      const res = this.normalizePagedResult(raw);

      this.people = res.items;
      this.totalPeople = res.total;
      this.notifySuccess(`Se cargaron ${res.items.length} personas.`);

      if (this.selectedPerson) {
        const updated = this.people.find(p => p.id === this.selectedPerson?.id) ?? null;
        this.selectPerson(updated);
      }
    });
  }

  async createPerson(): Promise<void> {
    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/people`;
      const created = await firstValueFrom(this.http.post<PersonDto>(url, this.personForm, { headers: this.authHeaders }));

      this.notifySuccess(`Persona creada: ${created.fullName}.`);
      this.selectPerson(created);
      await this.searchPeople();
    });
  }

  async updatePerson(): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Selecciona una persona para actualizar.');
      return;
    }

    await this.run(async () => {
      const payload = { id: this.selectedPerson!.id, ...this.personForm };
      const url = `${this.apiBaseUrl}/api/v1/people/${this.selectedPerson!.id}`;
      await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders }));

      this.notifySuccess('Persona actualizada.');
      await this.searchPeople();
    });
  }

  async toggleStatus(nextIsActive: boolean): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Selecciona una persona para cambiar su estado.');
      return;
    }

    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/people/${this.selectedPerson!.id}/status`;
      await firstValueFrom(this.http.patch<void>(url, { id: this.selectedPerson!.id, isActive: nextIsActive }, { headers: this.authHeaders }));

      this.notifySuccess(`Persona marcada como ${nextIsActive ? 'activa' : 'inactiva'}.`);
      await this.searchPeople();
    });
  }

  selectPerson(person: PersonDto | null): void {
    this.selectedPerson = person;
    this.riskScore = null;

    if (!person) {
      this.personForm = {
        fullName: '',
        identificationNumber: '',
        age: 18,
        gender: 0
      };
      this.personAttributes = [];
      return;
    }

    this.personForm = {
      fullName: person.fullName,
      identificationNumber: person.identificationNumber,
      age: person.age,
      gender: person.gender
    };

    void this.loadPersonAttributeForm();
  }

  async loadDefinitions(): Promise<void> {
    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/attributes/definitions`;
      const params = new HttpParams().set('onlyActive', String(!this.includeInactiveDefinitions));
      this.definitions = await firstValueFrom(
        this.http.get<AttributeDefinitionDto[]>(url, { headers: this.authHeaders, params })
      );
      this.hydrateDefinitionRuleDrafts();
      this.syncDynamicFiltersWithDefinitions();
      this.notifySuccess(`Se cargaron ${this.definitions.length} definiciones de atributos.`);
    });
  }

  async createDefinition(): Promise<void> {
    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/attributes/definitions`;
      const payload = {
        key: this.newDefinition.key.trim(),
        displayName: this.newDefinition.displayName.trim(),
        dataType: this.newDefinition.dataType,
        isFilterable: this.newDefinition.isFilterable,
        validationRulesJson: this.buildValidationRulesJson(this.newDefinition.dataType, this.newDefinitionRules)
      };

      await firstValueFrom(this.http.post<AttributeDefinitionDto>(url, payload, { headers: this.authHeaders }));

      this.newDefinition = {
        key: '',
        displayName: '',
        dataType: 1,
        isFilterable: true
      };
      this.newDefinitionRules = this.emptyRulesDraft();

      this.notifySuccess('Definicion de atributo creada.');
      await this.loadDefinitions();
    });
  }

  async updateDefinition(definition: AttributeDefinitionDto): Promise<void> {
    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/attributes/definitions/${definition.id}`;
      const payload = {
        id: definition.id,
        displayName: definition.displayName.trim(),
        isFilterable: definition.isFilterable,
        isActive: definition.isActive,
        validationRulesJson: this.buildValidationRulesJson(
          definition.dataType,
          this.definitionRulesDraft(definition.id, definition.dataType, definition.validationRulesJson)
        )
      };

      await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders }));
      definition.validationRulesJson = payload.validationRulesJson;
      this.notifySuccess(`Definicion guardada: ${definition.key}.`);

      if (this.selectedPerson) {
        await this.loadPersonAttributeForm();
      }
    });
  }

  async loadPersonAttributeForm(): Promise<void> {
    if (!this.selectedPerson) {
      return;
    }

    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/people/${this.selectedPerson!.id}/attributes/form`;
      const params = new HttpParams().set('onlyActive', 'true');

      this.personAttributes = await firstValueFrom(
        this.http.get<PersonAttributeFormItemDto[]>(url, { headers: this.authHeaders, params })
      );

      this.notifySuccess(`Se cargaron ${this.personAttributes.length} atributos para la persona seleccionada.`);
    });
  }

  async savePersonAttributes(): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Selecciona una persona primero.');
      return;
    }

    await this.run(async () => {
      const payload: UpsertAttributeValueDto[] = this.personAttributes.map(attr => {
        const out: UpsertAttributeValueDto = {
          key: attr.key,
          boolValue: null,
          stringValue: null,
          numberValue: null,
          dateValue: null
        };

        if (attr.dataType === 1) {
          out.boolValue = attr.boolValue;
        } else if (attr.dataType === 2 || attr.dataType === 5) {
          out.stringValue = this.nullIfEmpty(attr.stringValue ?? '');
        } else if (attr.dataType === 3) {
          out.numberValue = attr.numberValue;
        } else if (attr.dataType === 4) {
          out.dateValue = this.nullIfEmpty(attr.dateValue ?? '');
        }

        return out;
      });

      const url = `${this.apiBaseUrl}/api/v1/people/${this.selectedPerson!.id}/attributes`;
      await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders }));

      this.notifySuccess('Atributos de la persona guardados.');
      await this.loadPersonAttributeForm();
    });
  }

  async normalizeCondition(): Promise<void> {
    const text = this.conditionText.trim();
    if (!text) {
      this.notifyError('Ingresa un texto de condicion para normalizar.');
      return;
    }

    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/ai/conditions/normalize`;
      this.normalizedCondition = await firstValueFrom(
        this.http.post<NormalizeConditionResponseDto>(url, { text }, { headers: this.authHeaders })
      );
      this.notifySuccess(`Condicion normalizada como ${this.normalizedCondition.code}.`);
    });
  }

  async scoreSelectedPersonRisk(): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Selecciona una persona para calcular el riesgo.');
      return;
    }

    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/ai/people/${this.selectedPerson!.id}/risk-score`;
      const response = await firstValueFrom(
        this.http.post<RiskScoreResponseDto>(url, {}, { headers: this.authHeaders })
      );
      this.riskScore = {
        score: response.score,
        band: response.band,
        reasons: Array.isArray(response.reasons) ? response.reasons : []
      };
      this.notifySuccess(`Puntaje de riesgo calculado: ${this.riskScore.score} (${this.riskBandLabel(this.riskScore.band)}).`);
    });
  }

  applySuggestedAttributes(): void {
    if (!this.normalizedCondition) {
      this.notifyError('No hay resultado de condicion normalizada para aplicar.');
      return;
    }

    if (!this.selectedPerson) {
      this.notifyError('Selecciona una persona antes de aplicar atributos sugeridos.');
      return;
    }

    if (this.personAttributes.length === 0) {
      this.notifyError('Carga los atributos de la persona antes de aplicar sugerencias.');
      return;
    }

    const suggestions = this.normalizedCondition.suggestedAttributes;
    let applied = 0;
    console.log('Aplicando atributos sugeridos:', suggestions);
    for (const suggestion of suggestions) {
      const target = this.personAttributes.find(attr => attr.key === suggestion.key);
      if (!target) {
        continue;
      }

      target.boolValue = suggestion.boolValue;
      target.stringValue = suggestion.stringValue;
      target.numberValue = suggestion.numberValue;
      target.dateValue = suggestion.dateValue;
      applied += 1;
    }

    if (applied === 0) {
      this.notifyError('No se encontraron claves de atributos coincidentes para aplicar sugerencias.');
      return;
    }

    this.notifySuccess(`Se aplicaron ${applied} valor(es) de atributos sugeridos. Guarda los atributos para persistir.`);
    this.cdr.detectChanges();
  }

  addDynamicFilter(): void {
    this.peopleSearch.dynamicFilters.push({ key: '', value: '' });
  }

  removeDynamicFilter(index: number): void {
    this.peopleSearch.dynamicFilters.splice(index, 1);
    if (this.peopleSearch.dynamicFilters.length === 0) {
      this.peopleSearch.dynamicFilters.push({ key: '', value: '' });
    }
  }

  clearDynamicFilterDefinition(index: number): void {
    const filter = this.peopleSearch.dynamicFilters[index];
    if (!filter) {
      return;
    }

    filter.key = '';
    filter.value = '';
  }

  get filterableDefinitions(): AttributeDefinitionDto[] {
    return this.definitions
      .filter(def => def.isActive && def.isFilterable)
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  }

  onDynamicFilterKeyChange(index: number, key: string): void {
    const filter = this.peopleSearch.dynamicFilters[index];
    if (!filter) {
      return;
    }

    filter.key = key ?? '';
    filter.value = '';
  }

  dynamicFilterType(filter: DynamicFilterInput): AttributeDataType | null {
    return this.dynamicFilterDefinition(filter)?.dataType ?? null;
  }

  dynamicFilterAllowedValues(filter: DynamicFilterInput): string[] {
    const def = this.dynamicFilterDefinition(filter);
    if (!def || def.dataType !== 5 || !def.validationRulesJson) {
      return [];
    }

    try {
      const parsed = JSON.parse(def.validationRulesJson) as { allowedValues?: unknown; AllowedValues?: unknown };
      const rawAllowed = parsed.allowedValues ?? parsed.AllowedValues;
      if (!Array.isArray(rawAllowed)) {
        return [];
      }

      return rawAllowed
        .filter((x): x is string => typeof x === 'string')
        .map(x => x.trim())
        .filter(x => x.length > 0);
    } catch {
      return [];
    }
  }

  dynamicFilterValuePlaceholder(filter: DynamicFilterInput): string {
    const dataType = this.dynamicFilterType(filter);
    if (dataType === 1) {
      return 'Si o No';
    }
    if (dataType === 3) {
      return 'numero';
    }
    if (dataType === 4) {
      return 'fecha';
    }
    return 'valor';
  }

  definitionRulesForNewPreview(): string {
    return this.validationRulesPreview(this.newDefinition.dataType, this.newDefinitionRules);
  }

  definitionRulesForPreview(definition: AttributeDefinitionDto): string {
    const draft = this.definitionRulesDraft(definition.id, definition.dataType, definition.validationRulesJson);
    return this.validationRulesPreview(definition.dataType, draft);
  }

  definitionRulesDraft(
    definitionId: string,
    dataType: AttributeDataType,
    rawJson: string | null
  ): ValidationRulesDraft {
    if (!this.definitionRulesDrafts[definitionId]) {
      this.definitionRulesDrafts[definitionId] = this.parseRulesDraft(dataType, rawJson);
    }

    return this.definitionRulesDrafts[definitionId];
  }

  typeName(type: AttributeDataType): string {
    return this.attributeTypes.find(t => t.value === type)?.label ?? String(type);
  }

  isRiskBand(band: string | number, expected: 'low' | 'medium' | 'high'): boolean {
    return this.normalizeRiskBand(band) === expected;
  }

  riskBandLabel(band: string | number): string {
    const normalized = this.normalizeRiskBand(band);
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

  toggleTheme(): void {
    this.themeMode = this.themeMode === 'dark' ? 'light' : 'dark';
    localStorage.setItem(this.themeStorageKey, this.themeMode);
    this.cdr.detectChanges();
  }

  dismissNotification(id: number): void {
    this.notifications.update(items => items.filter(n => n.id !== id));
    this.cdr.detectChanges();
  }

  private get authHeaders(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.token}` });
  }

  private async run(work: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.cdr.detectChanges();

    try {
      await work();
    } catch (err: unknown) {
      this.notifyError(this.toErrorMessage(err));
    } finally {
      this.busy = false;
      this.cdr.detectChanges();
    }
  }

  private nullIfEmpty(value: string): string | null {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
  }

  private normalizePagedResult<T>(raw: PagedResultRaw<T>): PagedResult<T> {
    return {
      items: raw.items ?? raw.Items ?? [],
      total: raw.total ?? raw.Total ?? 0,
      page: raw.page ?? raw.Page ?? 1,
      pageSize: raw.pageSize ?? raw.PageSize ?? 20
    };
  }

  private syncDynamicFiltersWithDefinitions(): void {
    if (!this.peopleSearch.dynamicFilters.length) {
      this.peopleSearch.dynamicFilters.push({ key: '', value: '' });
      return;
    }

    const byKey = new Map(this.filterableDefinitions.map(def => [def.key, def]));

    for (const filter of this.peopleSearch.dynamicFilters) {
      if (filter.key && !byKey.has(filter.key)) {
        filter.key = '';
        filter.value = '';
      }
    }
  }

  private hydrateDefinitionRuleDrafts(): void {
    const next: Record<string, ValidationRulesDraft> = {};

    for (const def of this.definitions) {
      next[def.id] = this.definitionRulesDrafts[def.id] ?? this.parseRulesDraft(def.dataType, def.validationRulesJson);
    }

    this.definitionRulesDrafts = next;
  }

  private dynamicFilterDefinition(filter: DynamicFilterInput): AttributeDefinitionDto | null {
    if (!filter.key) {
      return null;
    }

    return this.filterableDefinitions.find(def => def.key === filter.key) ?? null;
  }

  private emptyRulesDraft(): ValidationRulesDraft {
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

  private parseRulesDraft(dataType: AttributeDataType, rawJson: string | null): ValidationRulesDraft {
    const draft = this.emptyRulesDraft();
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

  private buildValidationRulesJson(dataType: AttributeDataType, draft: ValidationRulesDraft): string | null {
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

  private validationRulesPreview(dataType: AttributeDataType, draft: ValidationRulesDraft): string {
    const json = this.buildValidationRulesJson(dataType, draft);
    return json ?? '(sin reglas)';
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

  private normalizeRiskBand(band: string | number): 'low' | 'medium' | 'high' | 'unknown' {
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

  private getInitialTheme(): 'light' | 'dark' {
    const stored = localStorage.getItem(this.themeStorageKey);
    if (stored === 'dark' || stored === 'light') {
      return stored;
    }

    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private notifySuccess(text: string): void {
    this.pushNotification('success', text);
  }

  private notifyError(text: string): void {
    this.pushNotification('error', text);
  }

  private pushNotification(type: 'success' | 'error', text: string): void {
    const id = Date.now() + Math.floor(Math.random() * 1000);
    this.notifications.update(items => [...items, { id, type, text }]);
    this.cdr.detectChanges();

    setTimeout(() => {
      this.dismissNotification(id);
    }, 3500);
  }

  private toErrorMessage(err: unknown): string {
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
