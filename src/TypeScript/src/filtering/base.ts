import { splitArguments } from '../funcs/index.js';
import type { FieldPath, FieldPathValue } from '../base.js';
import { CompositeOperator } from './CompositeOperator.js';

/**
 * Defines the available filter operators for individual property filtering conditions.
 * 
 * - `eq`: Tests for equality between the property value and the specified value
 * - `neq`: Tests for inequality between the property value and the specified value
 * - `gt`: Tests if the property value is greater than the specified value
 * - `gte`: Tests if the property value is greater than or equal to the specified value
 * - `lt`: Tests if the property value is less than the specified value
 * - `lte`: Tests if the property value is less than or equal to the specified value
 * - `ct`: Tests if the property value contains the specified substring
 * - `in`: Tests if the property value is in the specified array of values
 * - `sw`: Tests if the property value starts with the specified substring
 * - `ew`: Tests if the property value ends with the specified substring
 */
export type FilterOperator = 'eq' | 'neq' | 'gt' | 'gte' | 'lt' | 'lte' | 'ct' | 'in' | 'sw' | 'ew';

/**
 * Base interface for filtering information containing column and operator.
 * @template TData - The type of data being filtered
 */
export interface IFilteringInfo<TData extends object = any> {
  /** The column or field path being filtered */
  column: FieldPath<TData>;
  /** The filter operator to apply (defaults to 'eq' if not specified) */
  operator?: FilterOperator;
}

/**
 * Complete filtering interface extending IFilteringInfo with a value.
 * @template TData - The type of data being filtered
 * @template TColumn - The specific column/field path being filtered
 */
export interface IFiltering<TData extends object, TColumn extends FieldPath<TData> = FieldPath<TData>> extends IFilteringInfo<TData> {
  /** The value to filter by, typed according to the column type */
  value: FieldPathValue<TData, TColumn>;
}

/**
 * Interface for composite filtering operations that combine multiple conditions.
 */
export interface ICompositeFiltering {
  /** Array of string representations of filtering conditions */
  values: string[];
  /** The composite operator used to combine the conditions */
  compositeOperator: CompositeOperator;
}

/** Regular expression for parsing function-like expressions in format 'function(args)' */
export const funcRegex = /(\b[^()]+)\((.*)\)$/;

/** Regular expression for parsing collection operations in format 'collection:operator(args)' */
export const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;

/**
 * Parses a string expression into a filtering object.
 * Supports formats like 'property:operator(value)' or 'property:value'.
 * 
 * @template TData - The type of data being filtered
 * @param exp - The expression string to parse
 * @returns A filtering object with column, value, and operator
 * 
 * @example
 * ```typescript
 * parseExpression('name:eq(John)') // { column: 'name', value: 'John', operator: 'eq' }
 * parseExpression('age:25') // { column: 'age', value: '25', operator: 'eq' }
 * ```
 */
export function parseExpression<TData extends object = any>(exp: string): IFiltering<TData, FieldPath<TData>> {
  const arr = exp.split(':');
  const [prop, ...rest] = arr;
  const propertyName = prop.trim() as FieldPath<TData>;

  let operator: FilterOperator = 'eq';
  let value: string = '';

  if (rest.length > 0) {
    const v = rest.join(':');
    const match = v.match(funcRegex);
    if (match) {
      operator = match[1] as FilterOperator;
      value = match[2];
    } else value = v;
  }
  return { 
    column: propertyName, 
    value: value as FieldPathValue<TData, typeof propertyName>, 
    operator 
  };
}

/**
 * Parses a composite expression into a composite filtering object.
 * Supports both collection operations (collection:any/all(args)) and regular composite operations (and/or(args)).
 * 
 * @template TData - The type of data being filtered
 * @param exp - The composite expression string to parse
 * @param result - Array to store the parsed filtering results (modified by reference)
 * @returns A composite filtering object with values and operator, or null if parsing fails
 * 
 * @example
 * ```typescript
 * // Collection operation
 * parseComposite('roles:any(name:eq(Admin),isActive:eq(true))', [])
 * // Returns: { values: ['roles.name:eq(Admin)', 'roles.isActive:eq(true)'], compositeOperator: 'any' }
 * 
 * // Regular composite operation
 * parseComposite('and(name:eq(John),age:gt(25))', [])
 * // Returns: { values: ['name:eq(John)', 'age:gt(25)'], compositeOperator: 'and' }
 * ```
 */
export function parseComposite<TData extends object = any>(
  exp: string,
  result: IFiltering<TData, FieldPath<TData>>[],
): ICompositeFiltering | null {
  // Check for collection operations first (collection:any(args) or collection:all(args))
  const collectionMatch = exp.match(collectionRegex);
  if (collectionMatch) {
    const [, collectionProperty, collectionOperator, innerArgs] = collectionMatch;
    const compositeOperator = collectionOperator as CompositeOperator;
    
    // For collection operations, we need to wrap the inner args with the collection property
    const innerArguments = splitArguments(innerArgs);
    const wrappedArgs = innerArguments.map(arg => `${collectionProperty}.${arg}`);
    
    return { values: wrappedArgs, compositeOperator };
  }

  // Handle regular composite operations (and/or)
  const compositeMatch = exp.match(funcRegex);
  if (compositeMatch == null || compositeMatch.length <= 1 || compositeMatch[1].indexOf(':') >= 0) return null;

  const operator = compositeMatch[1] as CompositeOperator;
  if (!operator || (operator !== 'and' && operator !== 'or')) return null;

  const args = splitArguments(compositeMatch[2]);

  return { values: args, compositeOperator: operator };
}
