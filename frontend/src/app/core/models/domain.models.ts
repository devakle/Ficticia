export type Gender = 0 | 1 | 2 | 3;
export type AttributeDataType = 1 | 2 | 3 | 4 | 5;

export interface PersonDto {
  id: string;
  fullName: string;
  identificationNumber: string;
  age: number;
  gender: Gender;
  isActive: boolean;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AttributeDefinitionDto {
  id: string;
  key: string;
  displayName: string;
  dataType: AttributeDataType;
  isFilterable: boolean;
  isActive: boolean;
  validationRulesJson: string | null;
}

export interface PersonAttributeFormItemDto {
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

export interface UpsertAttributeValueDto {
  key: string;
  boolValue: boolean | null;
  stringValue: string | null;
  numberValue: number | null;
  dateValue: string | null;
}

export interface NormalizeConditionResponseDto {
  code: string;
  label: string;
  confidence: number;
  matchedTerms: string[];
  suggestedAttributes: UpsertAttributeValueDto[];
  source: string;
}

export interface RiskScoreResponseDto {
  score: number;
  band: string | number;
  reasons: string[] | null | undefined;
}

export interface DynamicFilterInput {
  key: string;
  value: string | number | null;
}

export interface ValidationRulesDraft {
  required: boolean;
  maxLength: number | null;
  regex: string;
  min: number | null;
  max: number | null;
  minDate: string;
  maxDate: string;
  allowedValuesText: string;
}

export interface PeopleSearchCriteria {
  name: string;
  identificationNumber: string;
  isActive: string;
  minAge: number | null;
  maxAge: number | null;
  page: number;
  pageSize: number;
  dynamicFilters: DynamicFilterInput[];
}
