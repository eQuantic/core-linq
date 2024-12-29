import type { FieldPath } from '../base.js';
import { FilterOperator, IFiltering } from './base.js';

export class Filtering<TData extends object = any> implements IFiltering<TData> {
  column: FieldPath<TData>;
  operator: FilterOperator;
  value: any;

  constructor();
  constructor(column: FieldPath<TData>, value: any, operator?: FilterOperator);
  constructor(...args: any[]) {
    this.column = args?.length > 0 ? args[0] : null;
    this.value = args?.length > 1 ? args[1] : null;
    this.operator = args?.length > 2 ? args[2] : 'eq';
  }

  toString(): string {
    return `${this.column}:${this.operator}(${this.value})`;
  }

  public static eq<TData extends object>(column: FieldPath<TData>, value: any): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'eq');
  }

  public static neq<TData extends object>(column: FieldPath<TData>, value: any): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'neq');
  }

  public static gt<TData extends object>(column: FieldPath<TData>, value: number | Date): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'gt');
  }

  public static lt<TData extends object>(column: FieldPath<TData>, value: number | Date): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'lt');
  }

  public static gte<TData extends object>(column: FieldPath<TData>, value: number | Date): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'gte');
  }

  public static lte<TData extends object>(column: FieldPath<TData>, value: number | Date): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'lte');
  }

  public static ct<TData extends object>(column: FieldPath<TData>, value: any): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'ct');
  }

  public static in<TData extends object>(column: FieldPath<TData>, ...value: any[]): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'in');
  }

  public static sw<TData extends object>(column: FieldPath<TData>, value: string): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'sw');
  }

  public static ew<TData extends object>(column: FieldPath<TData>, value: string): IFiltering<TData> {
    return new Filtering<TData>(column, value, 'ew');
  }
}

export const F = Filtering;
