import { splitArguments } from '../funcs/index.js';
import type { FieldPath } from '../base.js';
import { CompositeOperator } from './CompositeOperator.js';

export type FilterOperator = 'eq' | 'neq' | 'gt' | 'gte' | 'lt' | 'lte' | 'ct' | 'in' | 'sw' | 'ew';

export interface IFilteringInfo<TData extends object = any> {
  column: FieldPath<TData>;
  operator?: FilterOperator;
}

export interface IFiltering<TData extends object> extends IFilteringInfo<TData> {
  value: any;
}

export interface ICompositeFiltering {
  values: string[];
  compositeOperator: CompositeOperator;
}

export const funcRegex = /(\b[^()]+)\((.*)\)$/;

export function parseExpression<TData extends object = any>(exp: string): IFiltering<TData> {
  const arr = exp.split(':');
  const [prop, ...rest] = arr;
  const propertyName = prop.trim();

  let operator: FilterOperator = 'eq';
  let value = '';

  if (rest.length > 0) {
    const v = rest.join(':');
    const match = v.match(funcRegex);
    if (match) {
      operator = match[1] as FilterOperator;
      value = match[2];
    } else value = v;
  }
  return { column: propertyName as any, value, operator };
}

export function parseComposite<TData extends object = any>(
  exp: string,
  result: IFiltering<TData>[],
): ICompositeFiltering | null {
  const compositeMatch = exp.match(funcRegex);
  if (compositeMatch == null || compositeMatch.length <= 1 || compositeMatch[1].indexOf(':') >= 0) return null;

  const operator = compositeMatch[1] as CompositeOperator;
  if (!operator) return null;

  const args = splitArguments(compositeMatch[2]);

  return { values: args, compositeOperator: operator };
}
