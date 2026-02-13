import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
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

interface ToastMessage {
  id: number;
  type: 'success' | 'error';
  text: string;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly themeStorageKey = 'admin_theme_mode';

  readonly genders = [
    { value: 0, label: 'Unknown' },
    { value: 1, label: 'Male' },
    { value: 2, label: 'Female' },
    { value: 3, label: 'Other' }
  ];

  readonly attributeTypes = [
    { value: 1, label: 'Boolean' },
    { value: 2, label: 'String' },
    { value: 3, label: 'Number' },
    { value: 4, label: 'Date' },
    { value: 5, label: 'Enum' }
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
    dynamicFilters: [{ key: '', value: '' }]
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
    isFilterable: true,
    validationRulesJson: ''
  };

  personAttributes: PersonAttributeFormItemDto[] = [];

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
      this.notifySuccess('Authenticated successfully.');

      await Promise.all([this.searchPeople(), this.loadDefinitions()]);
    });
  }

  logout(): void {
    this.token = '';
    localStorage.removeItem('admin_token');
    this.notifySuccess('Token cleared.');
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
        const value = filter.value.trim();
        if (key && value) {
          params = params.set(`attr.${key}`, value);
        }
      }

      const url = `${this.apiBaseUrl}/api/v1/people`;
      const raw = await firstValueFrom(this.http.get<PagedResultRaw<PersonDto>>(url, { headers: this.authHeaders, params }));
      const res = this.normalizePagedResult(raw);

      this.people = res.items;
      this.totalPeople = res.total;
      this.notifySuccess(`Loaded ${res.items.length} people.`);

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

      this.notifySuccess(`Created person ${created.fullName}.`);
      this.selectPerson(created);
      await this.searchPeople();
    });
  }

  async updatePerson(): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Select a person to update.');
      return;
    }

    await this.run(async () => {
      const payload = { id: this.selectedPerson!.id, ...this.personForm };
      const url = `${this.apiBaseUrl}/api/v1/people/${this.selectedPerson!.id}`;
      await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders }));

      this.notifySuccess('Person updated.');
      await this.searchPeople();
    });
  }

  async toggleStatus(nextIsActive: boolean): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Select a person to change status.');
      return;
    }

    await this.run(async () => {
      const url = `${this.apiBaseUrl}/api/v1/people/${this.selectedPerson!.id}/status`;
      await firstValueFrom(this.http.patch<void>(url, { id: this.selectedPerson!.id, isActive: nextIsActive }, { headers: this.authHeaders }));

      this.notifySuccess(`Person marked as ${nextIsActive ? 'active' : 'inactive'}.`);
      await this.searchPeople();
    });
  }

  selectPerson(person: PersonDto | null): void {
    this.selectedPerson = person;

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
      this.notifySuccess(`Loaded ${this.definitions.length} attribute definitions.`);
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
        validationRulesJson: this.nullIfEmpty(this.newDefinition.validationRulesJson)
      };

      await firstValueFrom(this.http.post<AttributeDefinitionDto>(url, payload, { headers: this.authHeaders }));

      this.newDefinition = {
        key: '',
        displayName: '',
        dataType: 1,
        isFilterable: true,
        validationRulesJson: ''
      };

      this.notifySuccess('Attribute definition created.');
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
        validationRulesJson: this.nullIfEmpty(definition.validationRulesJson ?? '')
      };

      await firstValueFrom(this.http.put<void>(url, payload, { headers: this.authHeaders }));
      this.notifySuccess(`Saved definition ${definition.key}.`);

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

      this.notifySuccess(`Loaded ${this.personAttributes.length} attributes for selected person.`);
    });
  }

  async savePersonAttributes(): Promise<void> {
    if (!this.selectedPerson) {
      this.notifyError('Select a person first.');
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

      this.notifySuccess('Person attributes saved.');
      await this.loadPersonAttributeForm();
    });
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

  typeName(type: AttributeDataType): string {
    return this.attributeTypes.find(t => t.value === type)?.label ?? String(type);
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
      return status ? `Request failed (${status}): ${body}` : body;
    }

    if (body && typeof body === 'object') {
      const asRecord = body as Record<string, unknown>;
      const detail = asRecord['message'] ?? asRecord['title'] ?? JSON.stringify(body);
      return status ? `Request failed (${status}): ${detail}` : String(detail);
    }

    return status ? `Request failed (${status}).` : 'Unexpected error.';
  }
}
