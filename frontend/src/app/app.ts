import { DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, HostBinding, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AttributeDataType,
  AttributeDefinitionDto,
  DynamicFilterInput,
  Gender,
  NormalizeConditionResponseDto,
  PersonAttributeFormItemDto,
  PersonDto,
  RiskScoreResponseDto,
  UpsertAttributeValueDto,
  ValidationRulesDraft
} from './core/models/domain.models';
import { AuthApiService, LoginResult } from './core/services/auth-api.service';
import { ErrorMessageService } from './core/services/error-message.service';
import { ThemeService } from './core/services/theme.service';
import { ToastService } from './core/services/toast.service';
import { AiApiService } from './features/ai/services/ai-api.service';
import { RiskBandService } from './features/ai/services/risk-band.service';
import { AttributesApiService } from './features/attributes/services/attributes-api.service';
import { ValidationRulesService } from './features/attributes/services/validation-rules.service';
import { PeopleApiService } from './features/people/services/people-api.service';
import { PaginationItem, PeoplePaginationService } from './features/people/services/people-pagination.service';

const TOKEN_STORAGE_KEY = 'auth_token';
const LEGACY_TOKEN_STORAGE_KEY = 'admin_token';
const ROLES_STORAGE_KEY = 'auth_roles';

type DemoRole = 'Admin' | 'Manager' | 'Viewer';

interface DemoCredential {
  role: DemoRole;
  email: string;
  password: string;
  expectedAccess: string;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly authApi = inject(AuthApiService);
  private readonly peopleApi = inject(PeopleApiService);
  private readonly attributesApi = inject(AttributesApiService);
  private readonly aiApi = inject(AiApiService);
  private readonly riskBand = inject(RiskBandService);
  private readonly validationRules = inject(ValidationRulesService);
  private readonly toast = inject(ToastService);
  private readonly errorMessage = inject(ErrorMessageService);
  private readonly theme = inject(ThemeService);
  private readonly pagination = inject(PeoplePaginationService);

  readonly genders = [
    { value: 0, label: 'Desconocido' },
    { value: 1, label: 'Masculino' },
    { value: 2, label: 'Femenino' },
    { value: 3, label: 'Otro' }
  ];

  readonly attributeTypes = [
    { value: 1, label: 'Booleano' },
    { value: 2, label: 'Texto' },
    { value: 3, label: 'Número' },
    { value: 4, label: 'Fecha' },
    { value: 5, label: 'Enumerado' }
  ];

  readonly demoCredentials: DemoCredential[] = [
    {
      role: 'Admin',
      email: 'admin@ficticia.local',
      password: 'Admin123!',
      expectedAccess: 'Acceso total (personas, atributos e IA).'
    },
    {
      role: 'Manager',
      email: 'manager@ficticia.local',
      password: 'Manager123!',
      expectedAccess: 'Puede operar personas e IA, sin gestionar definiciones.'
    },
    {
      role: 'Viewer',
      email: 'viewer@ficticia.local',
      password: 'Viewer123!',
      expectedAccess: 'Solo lectura de personas e IA; sin operaciones de escritura.'
    }
  ];

  apiBaseUrl = '';
  email = 'admin@ficticia.local';
  password = 'Admin123!';
  token = this.readTokenFromStorage();
  currentRoles = this.readRolesFromStorage();
  themeMode: 'light' | 'dark' = this.theme.getInitialTheme();

  busy = false;
  notifications = this.toast.notifications;

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
  private savedFilterableDefinitions: AttributeDefinitionDto[] = [];
  newDefinition = {
    key: '',
    displayName: '',
    dataType: 1 as AttributeDataType,
    isFilterable: true
  };
  newDefinitionRules: ValidationRulesDraft = this.validationRules.emptyDraft();
  definitionRulesDrafts: Record<string, ValidationRulesDraft> = {};

  personAttributes: PersonAttributeFormItemDto[] = [];
  conditionText = '';
  normalizedCondition: NormalizeConditionResponseDto | null = null;
  riskScore: RiskScoreResponseDto | null = null;

  @HostBinding('class.dark-theme')
  get isDarkTheme(): boolean {
    return this.themeMode === 'dark';
  }

  constructor() {
    if (!this.token || this.currentRoles.length > 0) {
      return;
    }

    this.currentRoles = this.extractRolesFromJwt(this.token);
    this.persistRoles();
  }

  get currentRoleSummary(): string {
    return this.currentRoles.length ? this.currentRoles.join(', ') : 'Sin rol';
  }

  get hasPeopleWriteAccess(): boolean {
    return this.hasAnyRole('Admin', 'Manager');
  }

  get hasAttributesManageAccess(): boolean {
    return this.hasRole('Admin');
  }

  async loginAsDemo(credential: DemoCredential): Promise<void> {
    this.email = credential.email;
    this.password = credential.password;
    await this.login();
  }

  isRoleActive(role: string): boolean {
    return this.hasRole(role);
  }

  async login(): Promise<void> {
    await this.run(async () => {
      const auth = await this.authApi.login(this.apiBaseUrl, this.email, this.password);
      this.setAuthSession(auth);
      this.notifySuccess(`Autenticación exitosa (${this.currentRoleSummary}).`);

      await Promise.all([this.searchPeople(), this.loadDefinitions()]);
    });
  }

  logout(): void {
    this.token = '';
    this.currentRoles = [];
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(LEGACY_TOKEN_STORAGE_KEY);
    localStorage.removeItem(ROLES_STORAGE_KEY);
    this.people = [];
    this.totalPeople = 0;
    this.selectPerson(null);
    this.definitions = [];
    this.definitionRulesDrafts = {};
    this.syncDynamicFiltersWithDefinitions();
    this.notifySuccess('Token limpiado.');
  }

  async searchPeople(): Promise<void> {
    await this.run(async () => {
      this.peopleSearch.page = this.normalizePeoplePage(this.peopleSearch.page);
      this.peopleSearch.pageSize = this.normalizePeoplePageSize(this.peopleSearch.pageSize);
      const res = await this.peopleApi.search(this.apiBaseUrl, this.token, this.peopleSearch);

      this.people = res.items;
      this.totalPeople = res.total;
      this.peopleSearch.page = this.normalizePeoplePage(res.page);
      this.peopleSearch.pageSize = this.normalizePeoplePageSize(res.pageSize);
      this.notifySuccess(`Se cargaron ${res.items.length} personas.`);

      if (this.selectedPerson) {
        const updated = this.people.find(p => p.id === this.selectedPerson?.id) ?? null;
        this.selectPerson(updated);
      }
    });
  }

  async createPerson(): Promise<void> {
    await this.run(async () => {
      const created = await this.peopleApi.create(this.apiBaseUrl, this.token, this.personForm);

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
      const payload = {
        id: this.selectedPerson!.id,
        isActive: this.selectedPerson!.isActive,
        ...this.personForm
      };
      await this.peopleApi.update(this.apiBaseUrl, this.token, payload);

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
      await this.peopleApi.setStatus(this.apiBaseUrl, this.token, this.selectedPerson!.id, nextIsActive);

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
      this.definitions = await this.attributesApi.getDefinitions(this.apiBaseUrl, this.token, !this.includeInactiveDefinitions);
      this.hydrateDefinitionRuleDrafts();
      this.refreshSavedFilterableDefinitions();
      this.syncDynamicFiltersWithDefinitions();
      this.notifySuccess(`Se cargaron ${this.definitions.length} definiciones de atributos.`);
    });
  }

  async createDefinition(): Promise<void> {
    await this.run(async () => {
      const payload = {
        key: this.newDefinition.key.trim(),
        displayName: this.newDefinition.displayName.trim(),
        dataType: this.newDefinition.dataType,
        isFilterable: this.newDefinition.isFilterable,
        validationRulesJson: this.validationRules.buildJson(this.newDefinition.dataType, this.newDefinitionRules)
      };

      await this.attributesApi.createDefinition(this.apiBaseUrl, this.token, payload);

      this.newDefinition = {
        key: '',
        displayName: '',
        dataType: 1,
        isFilterable: true
      };
      this.newDefinitionRules = this.validationRules.emptyDraft();

      this.notifySuccess('Definición de atributo creada.');
      await this.loadDefinitions();
    });
  }

  async updateDefinition(definition: AttributeDefinitionDto): Promise<void> {
    await this.run(async () => {
      const payload = {
        id: definition.id,
        displayName: definition.displayName.trim(),
        isFilterable: definition.isFilterable,
        isActive: definition.isActive,
        validationRulesJson: this.validationRules.buildJson(
          definition.dataType,
          this.definitionRulesDraft(definition.id, definition.dataType, definition.validationRulesJson)
        )
      };

      await this.attributesApi.updateDefinition(this.apiBaseUrl, this.token, definition.id, payload);
      definition.validationRulesJson = payload.validationRulesJson;
      this.refreshSavedFilterableDefinitions();
      this.syncDynamicFiltersWithDefinitions();
      this.notifySuccess(`Definición guardada: ${definition.key}.`);

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
      this.personAttributes = await this.peopleApi.getAttributeForm(this.apiBaseUrl, this.token, this.selectedPerson!.id, true);

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

      await this.peopleApi.saveAttributes(this.apiBaseUrl, this.token, this.selectedPerson!.id, payload);

      this.notifySuccess('Atributos de la persona guardados.');
      await this.loadPersonAttributeForm();
    });
  }

  async normalizeCondition(): Promise<void> {
    const text = this.conditionText.trim();
    if (!text) {
      this.notifyError('Ingresa un texto de condición para normalizar.');
      return;
    }

    await this.run(async () => {
      this.normalizedCondition = await this.aiApi.normalizeCondition(this.apiBaseUrl, this.token, text);
      this.notifySuccess(`Condición normalizada como ${this.normalizedCondition.code}.`);
    });
  }

  async scoreSelectedPersonRisk(): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Selecciona una persona para calcular el riesgo.');
      return;
    }

    await this.run(async () => {
      const response = await this.aiApi.scorePersonRisk(this.apiBaseUrl, this.token, this.selectedPerson!.id);
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
      this.notifyError('No hay resultado de condición normalizada para aplicar.');
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

  get totalPeoplePages(): number {
    return this.pagination.totalPages(this.totalPeople, this.peopleSearch.pageSize);
  }

  get canGoToPreviousPeoplePage(): boolean {
    return this.peopleSearch.page > 1;
  }

  get canGoToNextPeoplePage(): boolean {
    return this.peopleSearch.page < this.totalPeoplePages;
  }

  get peopleFrom(): number {
    return this.pagination.visibleRange(this.totalPeople, this.peopleSearch.page, this.peopleSearch.pageSize, this.people.length).from;
  }

  get peopleTo(): number {
    return this.pagination.visibleRange(this.totalPeople, this.peopleSearch.page, this.peopleSearch.pageSize, this.people.length).to;
  }

  get peoplePaginationItems(): PaginationItem[] {
    return this.pagination.visiblePages(this.peopleSearch.page, this.totalPeoplePages, 3);
  }

  isPageNumber(item: PaginationItem): item is number {
    return typeof item === 'number';
  }

  goToPreviousPeoplePage(): void {
    this.goToPeoplePage(this.peopleSearch.page - 1);
  }

  goToNextPeoplePage(): void {
    this.goToPeoplePage(this.peopleSearch.page + 1);
  }

  goToPeoplePage(page: number): void {
    const nextPage = this.clampPeoplePage(page);
    if (nextPage === this.peopleSearch.page) {
      return;
    }

    this.peopleSearch.page = nextPage;
    void this.searchPeople();
  }

  onPeoplePageSizeChange(): void {
    const normalized = this.normalizePeoplePageSize(this.peopleSearch.pageSize);
    if (normalized === this.peopleSearch.pageSize && this.peopleSearch.page === 1) {
      return;
    }

    this.peopleSearch.pageSize = normalized;
    this.peopleSearch.page = 1;
    void this.searchPeople();
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
    return this.savedFilterableDefinitions;
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
    return this.validationRules.allowedValues(def.validationRulesJson);
  }

  dynamicFilterValuePlaceholder(filter: DynamicFilterInput): string {
    const dataType = this.dynamicFilterType(filter);
    if (dataType === 1) {
      return 'Si o No';
    }
    if (dataType === 3) {
      return 'número';
    }
    if (dataType === 4) {
      return 'fecha';
    }
    return 'valor';
  }

  definitionRulesForNewPreview(): string {
    return this.validationRules.preview(this.newDefinition.dataType, this.newDefinitionRules);
  }

  definitionRulesForPreview(definition: AttributeDefinitionDto): string {
    const draft = this.definitionRulesDraft(definition.id, definition.dataType, definition.validationRulesJson);
    return this.validationRules.preview(definition.dataType, draft);
  }

  definitionRulesDraft(
    definitionId: string,
    dataType: AttributeDataType,
    rawJson: string | null
  ): ValidationRulesDraft {
    if (!this.definitionRulesDrafts[definitionId]) {
      this.definitionRulesDrafts[definitionId] = this.validationRules.parseDraft(dataType, rawJson);
    }

    return this.definitionRulesDrafts[definitionId];
  }

  typeName(type: AttributeDataType): string {
    return this.attributeTypes.find(t => t.value === type)?.label ?? String(type);
  }

  isRiskBand(band: string | number, expected: 'low' | 'medium' | 'high'): boolean {
    return this.riskBand.isBand(band, expected);
  }

  riskBandLabel(band: string | number): string {
    return this.riskBand.label(band);
  }

  toggleTheme(): void {
    this.themeMode = this.theme.toggle(this.themeMode);
    this.cdr.detectChanges();
  }

  dismissNotification(id: number): void {
    this.toast.dismiss(id);
    this.cdr.detectChanges();
  }

  private async run(work: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.cdr.detectChanges();

    try {
      await work();
    } catch (err: unknown) {
      this.notifyError(this.errorMessage.toMessage(err));
    } finally {
      this.busy = false;
      this.cdr.detectChanges();
    }
  }

  private nullIfEmpty(value: string): string | null {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
  }

  private normalizePeoplePage(page: number): number {
    return this.pagination.normalizePage(page);
  }

  private normalizePeoplePageSize(pageSize: number): number {
    return this.pagination.normalizePageSize(pageSize);
  }

  private clampPeoplePage(page: number): number {
    return this.pagination.clampPage(page, this.totalPeople, this.peopleSearch.pageSize);
  }

  private syncDynamicFiltersWithDefinitions(): void {
    if (!this.peopleSearch.dynamicFilters.length) {
      this.peopleSearch.dynamicFilters.push({ key: '', value: '' });
      return;
    }

    const validKeys = new Set(this.filterableDefinitions.map(def => def.key));
    this.peopleSearch.dynamicFilters = this.peopleSearch.dynamicFilters.filter(filter => !filter.key || validKeys.has(filter.key));

    if (!this.peopleSearch.dynamicFilters.length) {
      this.peopleSearch.dynamicFilters.push({ key: '', value: '' });
    }
  }

  private refreshSavedFilterableDefinitions(): void {
    this.savedFilterableDefinitions = this.definitions
      .filter(def => def.isActive && def.isFilterable)
      .map(def => ({ ...def }))
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  }

  private hydrateDefinitionRuleDrafts(): void {
    const next: Record<string, ValidationRulesDraft> = {};

    for (const def of this.definitions) {
      next[def.id] = this.definitionRulesDrafts[def.id] ?? this.validationRules.parseDraft(def.dataType, def.validationRulesJson);
    }

    this.definitionRulesDrafts = next;
  }

  private dynamicFilterDefinition(filter: DynamicFilterInput): AttributeDefinitionDto | null {
    if (!filter.key) {
      return null;
    }

    return this.filterableDefinitions.find(def => def.key === filter.key) ?? null;
  }

  private hasRole(role: string): boolean {
    return this.currentRoles.some(current => current.toLowerCase() === role.toLowerCase());
  }

  private hasAnyRole(...roles: string[]): boolean {
    return roles.some(role => this.hasRole(role));
  }

  private setAuthSession(auth: LoginResult): void {
    this.token = auth.accessToken;
    this.currentRoles = this.normalizeRoles(auth.roles.length ? auth.roles : this.extractRolesFromJwt(auth.accessToken));

    localStorage.setItem(TOKEN_STORAGE_KEY, this.token);
    localStorage.removeItem(LEGACY_TOKEN_STORAGE_KEY);
    this.persistRoles();
  }

  private readTokenFromStorage(): string {
    return (localStorage.getItem(TOKEN_STORAGE_KEY) ?? localStorage.getItem(LEGACY_TOKEN_STORAGE_KEY) ?? '').trim();
  }

  private readRolesFromStorage(): string[] {
    const raw = localStorage.getItem(ROLES_STORAGE_KEY);
    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw);
      return this.normalizeRoles(Array.isArray(parsed) ? parsed : []);
    } catch {
      return [];
    }
  }

  private persistRoles(): void {
    localStorage.setItem(ROLES_STORAGE_KEY, JSON.stringify(this.currentRoles));
  }

  private normalizeRoles(roles: string[]): string[] {
    const normalized = roles
      .map(role => role.trim())
      .filter(role => !!role);

    return [...new Set(normalized)];
  }

  private extractRolesFromJwt(token: string): string[] {
    const parts = token.split('.');
    if (parts.length < 2) {
      return [];
    }

    try {
      const payload = JSON.parse(this.decodeBase64Url(parts[1]));
      const roleClaim = payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      if (Array.isArray(roleClaim)) {
        return this.normalizeRoles(roleClaim.filter((role): role is string => typeof role === 'string'));
      }

      if (typeof roleClaim === 'string') {
        return this.normalizeRoles([roleClaim]);
      }
    } catch {
      return [];
    }

    return [];
  }

  private decodeBase64Url(value: string): string {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
    return atob(padded);
  }

  private notifySuccess(text: string): void {
    this.toast.success(text);
    this.cdr.detectChanges();
  }

  private notifyError(text: string): void {
    this.toast.error(text);
    this.cdr.detectChanges();
  }
}
