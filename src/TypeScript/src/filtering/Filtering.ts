import type { FieldPath, FieldPathValue } from '../base.js';
import { FilterOperator, IFiltering } from './base.js';

/**
 * Represents a single filtering condition for a specific column/property.
 * Provides type-safe filtering with strongly-typed column paths and values.
 * 
 * @template TData - The type of data being filtered
 * @template TColumn - The specific column/field path being filtered
 */
export class Filtering<TData extends object = any, TColumn extends FieldPath<TData> = FieldPath<TData>> implements IFiltering<TData, TColumn> {
  /** The column or field path being filtered */
  column: TColumn;
  
  /** The filter operator to apply */
  operator: FilterOperator;
  
  /** The value to filter by, typed according to the column type */
  value: FieldPathValue<TData, TColumn>;

  /** Creates an empty filtering instance */
  constructor();
  
  /**
   * Creates a new filtering instance with the specified column, value, and operator.
   * @param column - The column or field path to filter
   * @param value - The value to filter by
   * @param operator - The filter operator to use (defaults to 'eq')
   */
  constructor(column: TColumn, value: FieldPathValue<TData, TColumn>, operator?: FilterOperator);
  
  constructor(...args: any[]) {
    this.column = args?.length > 0 ? args[0] : (null as unknown as TColumn);
    this.value = args?.length > 1 ? args[1] : (null as unknown as FieldPathValue<TData, TColumn>);
    this.operator = args?.length > 2 ? args[2] : 'eq';
  }

  /**
   * Converts the filtering instance to its string representation.
   * @returns A string representation in the format 'column:operator(value)'
   */
  toString(): string {
    return `${this.column}:${this.operator}(${this.value})`;
  }

  /**
   * Creates a new filtering instance with equality operator.
   * @template TData - The type of data being filtered
   * @template TColumn - The specific column/field path being filtered
   * @param column - The column or field path to filter
   * @param value - The value to test for equality
   * @returns A new filtering instance with equality operation
   */
  public static eq<TData extends object, TColumn extends FieldPath<TData>>(
    column: TColumn,
    value: FieldPathValue<TData, TColumn>
  ): IFiltering<TData, TColumn> {
    return new Filtering<TData, TColumn>(column, value, 'eq');
  }

  /**
   * Creates a new filtering instance with inequality operator.
   * @template TData - The type of data being filtered
   * @template TColumn - The specific column/field path being filtered
   * @param column - The column or field path to filter
   * @param value - The value to test for inequality
   * @returns A new filtering instance with inequality operation
   */
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

  /**
   * Creates a new filtering instance with contains operator (for string properties).
   * @template TData - The type of data being filtered
   * @template TColumn - The specific column/field path being filtered
   * @param column - The column or field path to filter
   * @param value - The substring to search for within the property value
   * @returns A new filtering instance with contains operation
   */
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

/**
 * Alias for Filtering class for more concise usage.
 * @example
 * ```typescript
 * // Using full class name
 * const filter1 = Filtering.eq('name', 'John');
 * 
 * // Using alias
 * const filter2 = F.eq('name', 'John');
 * ```
 */
export const F = Filtering;
