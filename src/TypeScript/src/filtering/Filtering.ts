import type { FieldPath, FieldPathValue } from '../base.js';
import { FilterOperator, IFiltering } from './base.js';

export class Filtering<TData extends object = any, TColumn extends FieldPath<TData> = FieldPath<TData>> implements IFiltering<TData, TColumn> {
  column: TColumn;
  operator: FilterOperator;
  value: FieldPathValue<TData, TColumn>;

  constructor();
  constructor(column: TColumn, value: FieldPathValue<TData, TColumn>, operator?: FilterOperator);
  constructor(...args: any[]) {
    this.column = args?.length > 0 ? args[0] : (null as unknown as TColumn);
    this.value = args?.length > 1 ? args[1] : (null as unknown as FieldPathValue<TData, TColumn>);
    this.operator = args?.length > 2 ? args[2] : 'eq';
  }

  toString(): string {
    return `${this.column}:${this.operator}(${this.value})`;
  }

  public static eq<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn>
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value, 'eq');
  }

  public static neq<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn>
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value, 'neq');
  }

  public static gt<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends number | Date ? FieldPathValue<TData, TColumn> : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'gt');
  }

  public static lt<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends number | Date ? FieldPathValue<TData, TColumn> : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'lt');
  }

  public static gte<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends number | Date ? FieldPathValue<TData, TColumn> : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'gte');
  }

  public static lte<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends number | Date ? FieldPathValue<TData, TColumn> : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'lte');
  }

  public static ct<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends string ? string : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'ct');
  }

  public static in<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    ...value: FieldPathValue<TData, TColumn>[]
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'in');
  }

  public static sw<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends string ? string : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'sw');
  }

  public static ew<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn> extends string ? string : never
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value as FieldPathValue<TData, TColumn>, 'ew');
  }
}

export const F = Filtering;
