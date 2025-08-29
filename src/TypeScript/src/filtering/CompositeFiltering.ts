import { CompositeOperator } from './CompositeOperator.js';
import { Filtering } from './Filtering.js';
import { IFiltering, parseExpression, parseComposite } from './base.js';
import type { FieldPath } from '../base.js';

/**
 * Represents a composite filtering operation that combines multiple filtering conditions using logical operators.
 * Supports And/Or operations for regular conditions and Any/All operations for collection-based filtering.
 * 
 * @template TData - The type of data being filtered
 * @template TColumn - The type of column/field path being filtered
 */
export class CompositeFiltering<TData extends object = any, TColumn extends FieldPath<TData> = FieldPath<TData>> extends Filtering<TData, TColumn> {
  /** Array of filtering conditions to be combined using the composite operator */
  values: IFiltering<TData>[];
  
  /** The composite operator used to combine the filtering conditions */
  compositeOperator: CompositeOperator;

  /**
   * Creates a new CompositeFiltering instance with the specified operator and filter values.
   * @param compositeOperator - The composite operator to use for combining the conditions
   * @param values - Array of string filter expressions or filtering instances to be combined
   */
  constructor(compositeOperator: CompositeOperator, values?: IFiltering<TData>[] | string[]);
  
  /**
   * Creates a new CompositeFiltering instance with a specific column and filter values.
   * This overload is typically used for Any/All operations on collection properties.
   * @param compositeOperator - The composite operator to use for combining the conditions
   * @param column - The column or property this composite filter applies to
   * @param values - Array of string filter expressions or filtering instances to be combined
   */
  constructor(compositeOperator: CompositeOperator, column: TColumn, values: IFiltering<TData>[] | string[]);
  
  constructor(...args: any[]) {
    // Handle different constructor overloads for the super class
    let superColumn: TColumn | undefined;
    
    if (args.length === 3 && Array.isArray(args[2])) {
      // Constructor(operator, column, values) - pass column to super
      superColumn = args[1];
    }
    
    super(superColumn as TColumn, null as any, 'eq'); // Super constructor needs column
    
    this.compositeOperator = args.length > 0 ? args[0] : 'and';
    this.values = [];
    
    // Handle different constructor overloads
    let valuesArray: IFiltering<TData>[] | string[] = [];
    
    if (args.length === 2 && Array.isArray(args[1])) {
      // Constructor(operator, values)
      valuesArray = args[1];
    } else if (args.length === 3 && Array.isArray(args[2])) {
      // Constructor(operator, column, values)
      this.column = args[1];
      valuesArray = args[2];
    }
    
    valuesArray.forEach((value) => {
      if (typeof value === 'string') {
        const composite = parseComposite<TData>(value, this.values);
        if (!composite) {
          const parsed = parseExpression<TData>(value);
          const f = new Filtering<TData>(parsed.column, parsed.value, parsed.operator);
          this.values.push(f);
        }
      } else this.values.push(value);
    });
  }

  /**
   * Adds a filtering condition to the composite filter.
   * @param filtering - The filtering condition to add
   */
  addValue(filtering: IFiltering<TData>) {
    this.values.push(filtering);
  }

  /**
   * Converts the composite filtering instance to its string representation.
   * For collection operations (Any/All), formats as 'collection:operator(args)'.
   * For regular operations (And/Or), formats as 'operator(args)'.
   * @returns A string representation of the composite filtering operation
   */
  toString(): string {
    if (this.values.length == 0) return super.toString();
    const args = this.values.map((v) => v.toString());
    
    // Handle collection operations (any/all) with explicit column
    if (this.column && (this.compositeOperator === 'any' || this.compositeOperator === 'all')) {
      return `${this.column}:${this.compositeOperator}(${args.join(',')})`;
    }
    
    // Handle collection operations (any/all) by extracting from args
    if (this.compositeOperator === 'any' || this.compositeOperator === 'all') {
      // For collection operations, we need to extract the collection property from the first value
      if (args.length > 0 && args[0].includes('.')) {
        const firstArg = args[0];
        const dotIndex = firstArg.indexOf('.');
        if (dotIndex > 0) {
          const collectionProperty = firstArg.substring(0, dotIndex);
          // Remove collection property prefix from all args
          const innerArgs = args.map(arg => {
            if (arg.startsWith(`${collectionProperty}.`)) {
              return arg.substring(collectionProperty.length + 1);
            }
            return arg;
          });
          return `${collectionProperty}:${this.compositeOperator}(${innerArgs.join(',')})`;
        }
      }
    }
    
    // Handle regular composite operations (and/or)
    return `${this.compositeOperator}(${args.join(',')})`;
  }

  /**
   * Creates a new CompositeFiltering instance using the AND operator.
   * All specified conditions must be true for the filter to match.
   * @template TData - The type of data being filtered
   * @param values - Array of string filter expressions or filtering instances to combine with AND
   * @returns A new CompositeFiltering instance with AND operation
   */
  public static and<TData extends object>(...values: IFiltering<TData>[] | string[]): CompositeFiltering<TData> {
    return new CompositeFiltering<TData>('and', values);
  }

  /**
   * Creates a new CompositeFiltering instance using the OR operator.
   * At least one of the specified conditions must be true for the filter to match.
   * @template TData - The type of data being filtered
   * @param values - Array of string filter expressions or filtering instances to combine with OR
   * @returns A new CompositeFiltering instance with OR operation
   */
  public static or<TData extends object>(...values: IFiltering<TData>[] | string[]): CompositeFiltering<TData> {
    return new CompositeFiltering<TData>('or', values);
  }

  /**
   * Creates a new CompositeFiltering instance using the ANY operator for collections.
   * Returns true if any item in the collection matches the specified conditions.
   * @template TData - The type of data being filtered
   * @param values - Array of string filter expressions or filtering instances for collection items
   * @returns A new CompositeFiltering instance with ANY operation
   */
  public static any<TData extends object>(...values: IFiltering<TData>[] | string[]): CompositeFiltering<TData> {
    return new CompositeFiltering<TData>('any', values);
  }

  /**
   * Creates a new CompositeFiltering instance using the ALL operator for collections.
   * Returns true if all items in the collection match the specified conditions.
   * @template TData - The type of data being filtered
   * @param values - Array of string filter expressions or filtering instances for collection items
   * @returns A new CompositeFiltering instance with ALL operation
   */
  public static all<TData extends object>(...values: IFiltering<TData>[] | string[]): CompositeFiltering<TData> {
    return new CompositeFiltering<TData>('all', values);
  }
}

/**
 * Alias for CompositeFiltering class for more concise usage.
 * @example
 * ```typescript
 * // Using full class name
 * const filter1 = CompositeFiltering.and('name:eq(John)', 'age:gt(25)');
 * 
 * // Using alias
 * const filter2 = CF.and('name:eq(John)', 'age:gt(25)');
 * ```
 */
export const CF = CompositeFiltering;
